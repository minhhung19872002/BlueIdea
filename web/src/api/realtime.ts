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
