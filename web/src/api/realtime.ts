import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import type { HubConnection } from '@microsoft/signalr';

import { boNhoToken } from './client';

/** Tên sự kiện máy chủ đẩy xuống — phải khớp `ThongBaoHub.SuKien`. */
export const SU_KIEN_REALTIME = {
  thongBaoMoi: 'ThongBaoMoi',
  capNhatTrangThaiHoSo: 'CapNhatTrangThaiHoSo',
  capNhatBangDiem: 'CapNhatBangDiem',
  ketQuaKiemTraTrungLap: 'KetQuaKiemTraTrungLap',
} as const;

let ketNoi: HubConnection | null = null;

/**
 * Kết nối tới hub thông báo.
 *
 * Dùng CHUNG một kết nối cho cả ứng dụng: mỗi màn hình mở một kết nối riêng sẽ tạo hàng chục
 * WebSocket cho cùng một người dùng. Máy chủ chỉ đẩy *tín hiệu*, client nhận rồi tự gọi API lấy
 * dữ liệu theo đúng quyền của mình.
 */
export function layKetNoiRealtime(): HubConnection {
  if (ketNoi) return ketNoi;

  ketNoi = new HubConnectionBuilder()
    .withUrl(`${import.meta.env.VITE_API_URL ?? ''}/hubs/thong-bao`, {
      accessTokenFactory: () => boNhoToken.layAccessToken() ?? '',
    })
    // Tự kết nối lại khi mạng chập chờn; khoảng lùi tăng dần để không dội máy chủ.
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(LogLevel.Warning)
    .build();

  return ketNoi;
}

/** Bắt đầu kết nối nếu chưa chạy. An toàn khi gọi nhiều lần. */
export async function batDauRealtime(): Promise<HubConnection | null> {
  const kn = layKetNoiRealtime();

  if (kn.state === HubConnectionState.Connected || kn.state === HubConnectionState.Connecting) {
    return kn;
  }

  try {
    await kn.start();
    return kn;
  } catch {
    // Không kết nối được realtime thì giao diện vẫn chạy bình thường bằng cách hỏi lại theo
    // chu kỳ — realtime là phần tăng thêm, không phải điều kiện để dùng hệ thống.
    return null;
  }
}

/** Ngắt kết nối khi đăng xuất — token cũ không còn dùng được nữa. */
export async function dungRealtime() {
  if (!ketNoi) return;

  try {
    await ketNoi.stop();
  } finally {
    ketNoi = null;
  }
}

/**
 * Theo dõi realtime một hồ sơ: trạng thái đổi, hoặc kiểm tra trùng lặp vừa chạy xong.
 *
 * Kiểm tra trùng lặp chạy nền (ngay khi nộp, và job quét bù mỗi 15 phút), nên không có tín hiệu
 * này thì người mở tab Trùng lặp ngồi nhìn dòng "đang kiểm tra" mãi cho tới khi tự bấm tải lại.
 *
 * Cùng khuôn với `theoDoiPhienHop`: trả về hàm huỷ đăng ký và tự vào lại nhóm sau khi kết nối
 * lại, vì SignalR khôi phục kết nối nhưng KHÔNG khôi phục nhóm.
 */
export function theoDoiHoSo(
  sangKienId: string,
  khiCapNhat: (suKien: 'trang-thai' | 'trung-lap') => void,
): () => void {
  let daHuy = false;
  const kn = layKetNoiRealtime();

  const thamGia = () => {
    void kn.invoke('ThamGiaHoSo', sangKienId).catch(() => {
      // Chưa kết nối xong thì lần reconnect kế tiếp sẽ tham gia lại.
    });
  };

  const khiDoiTrangThai = () => {
    if (!daHuy) khiCapNhat('trang-thai');
  };

  const khiCoTrungLap = () => {
    if (!daHuy) khiCapNhat('trung-lap');
  };

  kn.on(SU_KIEN_REALTIME.capNhatTrangThaiHoSo, khiDoiTrangThai);
  kn.on(SU_KIEN_REALTIME.ketQuaKiemTraTrungLap, khiCoTrungLap);
  kn.onreconnected(thamGia);

  void batDauRealtime().then(() => {
    if (!daHuy) thamGia();
  });

  return () => {
    daHuy = true;
    kn.off(SU_KIEN_REALTIME.capNhatTrangThaiHoSo, khiDoiTrangThai);
    kn.off(SU_KIEN_REALTIME.ketQuaKiemTraTrungLap, khiCoTrungLap);
    void kn.invoke('RoiHoSo', sangKienId).catch(() => {
      // Kết nối đã đóng thì máy chủ tự dọn nhóm.
    });
  };
}

/**
 * Theo dõi realtime một phòng họp hội đồng.
 *
 * Trả về hàm huỷ đăng ký — LUÔN gọi khi rời phiên: không rời nhóm thì sau vài lần mở/đóng phiên,
 * một trình duyệt vẫn nhận tín hiệu của những phiên nó không còn xem và nạp lại dữ liệu vô ích.
 *
 * Tự đăng ký lại sau khi kết nối lại: SignalR khôi phục kết nối nhưng KHÔNG khôi phục nhóm, bỏ
 * bước này thì mất mạng một nhịp là phòng họp im lặng cho tới khi tải lại trang.
 */
export function theoDoiPhienHop(phienHopId: string, khiCapNhat: () => void): () => void {
  let daHuy = false;
  const kn = layKetNoiRealtime();

  const thamGia = () => {
    void kn.invoke('ThamGiaPhienHop', phienHopId).catch(() => {
      // Chưa kết nối xong thì lần reconnect kế tiếp sẽ tham gia lại.
    });
  };

  const xuLy = () => {
    if (!daHuy) khiCapNhat();
  };

  kn.on(SU_KIEN_REALTIME.capNhatBangDiem, xuLy);
  kn.onreconnected(thamGia);

  void batDauRealtime().then(() => {
    if (!daHuy) thamGia();
  });

  return () => {
    daHuy = true;
    kn.off(SU_KIEN_REALTIME.capNhatBangDiem, xuLy);
    void kn.invoke('RoiPhienHop', phienHopId).catch(() => {
      // Kết nối đã đóng thì máy chủ tự dọn nhóm.
    });
  };
}
