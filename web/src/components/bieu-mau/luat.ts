import { z } from 'zod';

/**
 * Các luật kiểm tra dùng lại nhiều lần, kèm sẵn thông báo tiếng Việt.
 *
 * Gom một chỗ để mọi màn hình báo lỗi giống nhau: trước đây mỗi form tự viết `message` riêng nên
 * cùng một lỗi "thiếu mã" lại hiện ba câu khác nhau tuỳ màn hình.
 */

/** Chuỗi bắt buộc, tự cắt khoảng trắng thừa hai đầu. */
export const batBuoc = (nhan: string, toiDa = 300) =>
  z
    .string({ required_error: `Vui lòng nhập ${nhan.toLowerCase()}.` })
    .trim()
    .min(1, `Vui lòng nhập ${nhan.toLowerCase()}.`)
    .max(toiDa, `${nhan} không quá ${toiDa} ký tự.`);

/** Chuỗi không bắt buộc: ô để trống trả về undefined thay vì chuỗi rỗng. */
export const tuyChon = (toiDa = 1000) =>
  z
    .string()
    .trim()
    .max(toiDa, `Không quá ${toiDa} ký tự.`)
    .optional()
    .or(z.literal('').transform(() => undefined));

/**
 * Mã danh mục: chữ hoa, số, gạch dưới và gạch ngang.
 *
 * Chặn dấu cách và chữ thường ngay từ giao diện vì mã được dùng làm khoá khi nhập Excel và khi
 * đối chiếu với hệ thống ngoài — sửa mã sau khi đã có dữ liệu là việc rất tốn công.
 */
export const maDanhMuc = (toiDa = 50) =>
  batBuoc('Mã', toiDa).regex(
    /^[A-Z0-9_-]+$/,
    'Mã chỉ gồm chữ in hoa không dấu, số, dấu gạch dưới và gạch ngang.',
  );

/** Mã có cả chữ thường (ví dụ tên đăng nhập, mã vai trò). */
export const maKyThuat = (toiDa = 50) =>
  batBuoc('Mã', toiDa).regex(
    /^[A-Za-z0-9_.-]+$/,
    'Chỉ gồm chữ không dấu, số và các ký tự . _ -',
  );

/** Số nguyên trong khoảng, chấp nhận ô để trống (trả về undefined). */
export const soNguyen = (nhan: string, tu: number, den: number) =>
  z
    .number({ invalid_type_error: `${nhan} phải là số.` })
    .int(`${nhan} phải là số nguyên.`)
    .min(tu, `${nhan} phải từ ${tu}.`)
    .max(den, `${nhan} không quá ${den}.`);

/** Trạng thái danh mục: 1 = đang dùng, 0 = ngừng. */
export const trangThai = z.union([z.literal(0), z.literal(1)]);

export const email = z
  .string()
  .trim()
  .email('Địa chỉ email không hợp lệ.')
  .optional()
  .or(z.literal('').transform(() => undefined));

/** Số điện thoại Việt Nam: 10 số, bắt đầu bằng 0. */
export const dienThoai = z
  .string()
  .trim()
  .regex(/^0\d{9}$/, 'Số điện thoại phải gồm 10 chữ số và bắt đầu bằng 0.')
  .optional()
  .or(z.literal('').transform(() => undefined));
