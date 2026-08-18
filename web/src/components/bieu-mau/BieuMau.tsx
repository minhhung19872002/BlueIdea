import { Controller, FormProvider, useFormContext } from 'react-hook-form';
import type {
  Control,
  DefaultValues,
  FieldPath,
  FieldValues,
  UseFormReturn,
} from 'react-hook-form';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Form } from 'antd';
import type { FormItemProps } from 'antd';
import type { ZodType } from 'zod';

/**
 * Lớp nối react-hook-form + zod với giao diện Ant Design.
 *
 * Vì sao có lớp này thay vì dùng thẳng react-hook-form ở từng màn hình: nếu mỗi form tự viết
 * `<Controller>` rồi tự ánh xạ lỗi sang `validateStatus` / `help` thì cùng một việc bị chép lại
 * hàng chục lần, và chỉ cần một chỗ quên `validateStatus` là ô đó báo lỗi mà không tô đỏ — người
 * dùng không thấy mình sai ở đâu.
 *
 * Ant Design giữ nguyên vai trò TRÌNH BÀY (nhãn, bố cục, khoảng cách); react-hook-form giữ trạng
 * thái và zod giữ luật kiểm tra. Nhờ vậy luật kiểm tra nằm MỘT CHỖ và dùng lại được cho cả kiểu
 * dữ liệu TypeScript, thay vì rải trong prop `rules` của từng ô.
 */

/** Tạo form gắn với một schema zod. Kiểu dữ liệu suy ra thẳng từ schema. */
export function useBieuMau<T extends FieldValues>(
  schema: ZodType<T>,
  giaTriMacDinh?: DefaultValues<T>,
) {
  return useForm<T>({
    resolver: zodResolver(schema as never),
    defaultValues: giaTriMacDinh,
    // Kiểm tra khi rời ô rồi báo lại theo từng phím: gõ dở đã tô đỏ thì rất khó chịu, nhưng khi
    // ô đã sai một lần thì người dùng cần thấy nó hết sai ngay lúc sửa xong.
    mode: 'onBlur',
    reValidateMode: 'onChange',
  });
}

interface PropsBieuMau<T extends FieldValues> extends React.PropsWithChildren {
  form: UseFormReturn<T>;
  onGui: (giaTri: T) => void | Promise<unknown>;
  layout?: 'vertical' | 'horizontal' | 'inline';
  id?: string;
}

/**
 * Thẻ bọc form.
 *
 * Chặn gửi trùng ngay tại đây (`isSubmitting`) chứ không để từng màn hình tự lo: bấm hai lần vào
 * nút Lưu là tạo ra hai bản ghi, và đó là lỗi rất dễ lọt vì máy nhanh thì không tái hiện được.
 */
export function BieuMau<T extends FieldValues>({
  form,
  onGui,
  layout = 'vertical',
  id,
  children,
}: PropsBieuMau<T>) {
  return (
    <FormProvider {...form}>
      <Form
        id={id}
        layout={layout}
        component="form"
        onFinish={undefined}
        onSubmitCapture={form.handleSubmit(async (giaTri) => {
          await onGui(giaTri);
        })}
      >
        {children}
      </Form>
    </FormProvider>
  );
}

interface PropsTruong<T extends FieldValues>
  extends Omit<FormItemProps, 'name' | 'children' | 'rules'> {
  ten: FieldPath<T>;
  control?: Control<T>;
  /** Nhận giá trị và hàm đổi giá trị, trả về đúng ô nhập của Ant Design. */
  children: (o: {
    value: unknown;
    onChange: (...su: unknown[]) => void;
    onBlur: () => void;
    status?: 'error';
  }) => React.ReactNode;
}

/**
 * Một ô nhập: nối `Controller` của react-hook-form vào `Form.Item` của Ant Design.
 *
 * Dấu bắt buộc (*) suy ra từ schema chứ không khai tay: khai tay thì sửa schema mà quên sửa nhãn
 * sẽ ra ô có dấu sao nhưng không bắt buộc, hoặc ngược lại — cả hai đều làm người dùng mất lòng tin.
 */
export function Truong<T extends FieldValues>({
  ten,
  control,
  children,
  ...conLai
}: PropsTruong<T>) {
  const ngamForm = useFormContext<T>();
  const dieuKhien = control ?? ngamForm.control;

  return (
    <Controller
      name={ten}
      control={dieuKhien}
      render={({ field, fieldState }) => (
        <Form.Item
          {...conLai}
          validateStatus={fieldState.error ? 'error' : undefined}
          help={fieldState.error?.message}
        >
          {children({
            value: field.value,
            onChange: field.onChange,
            onBlur: field.onBlur,
            status: fieldState.error ? 'error' : undefined,
          })}
        </Form.Item>
      )}
    />
  );
}
