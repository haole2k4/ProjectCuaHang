# Product Management Feature

## Tính n?ng CRUD ?ã tri?n khai

### 1. **Thêm s?n ph?m m?i (Create)**
- Click nút "Add Product" ? góc trên bên ph?i
- ?i?n thông tin s?n ph?m:
  - **Product Name** (b?t bu?c): Tên s?n ph?m
  - **Barcode** (tùy ch?n): Mã v?ch - n?u ?? tr?ng s? t? ??ng t?o
  - **Category**: Ch?n danh m?c s?n ph?m
  - **Price** (b?t bu?c): Giá bán, ph?i l?n h?n 0
  - **Unit**: ??n v? tính (m?c ??nh là "pcs")
  - **Description**: Mô t? s?n ph?m
  - **Product Image**: Upload hình ?nh s?n ph?m
    - Ch?p nh?n: PNG, JPG, JPEG
    - Kích th??c t?i ?a: 5MB
    - Hình ?nh ???c l?u trong th? m?c `wwwroot/Statics/`

### 2. **Xem danh sách s?n ph?m (Read)**
- Hi?n th? b?ng v?i các thông tin:
  - Hình ?nh s?n ph?m
  - Tên s?n ph?m và barcode
  - Danh m?c
  - Giá bán
  - S? l??ng t?n kho
  - Tr?ng thái (active/inactive)
- Giao di?n v?i màu s?c t??ng ph?n cao:
  - Header b?ng: n?n ?en (#1F2937), ch? tr?ng
  - N?i dung: ch? ?en/xám ??m trên n?n tr?ng
  - Hover effect: n?n xám nh?t

### 3. **C?p nh?t s?n ph?m (Update)**
- Click nút Edit (bi?u t??ng bút) ? c?t Actions
- S?a thông tin c?n thi?t
- Click "Update" ?? l?u thay ??i
- H? tr? thay ??i hình ?nh

### 4. **Xóa s?n ph?m (Delete)**
- Click nút Delete (bi?u t??ng thùng rác) ? c?t Actions
- Xác nh?n xóa
- Hai lo?i xóa:
  - **Xóa v?nh vi?n**: N?u s?n ph?m ch?a t?ng ???c bán
  - **Soft delete**: N?u s?n ph?m ?ã có trong ??n hàng ? chuy?n status sang "inactive"

### 5. **Upload và qu?n lý hình ?nh**
- Hình ?nh ???c l?u trong `wwwroot/Statics/`
- Tên file ???c t?o t? ??ng dùng GUID ?? tránh trùng l?p
- H? tr? xóa hình ?nh c? khi upload hình m?i
- Xóa file ?nh khi xóa s?n ph?m

## C?i ti?n giao di?n

### Màu s?c t??ng ph?n cao
- **Header b?ng**: N?n xám ??m (#374151), ch? tr?ng
- **N?i dung b?ng**: Ch? ?en (#111827) trên n?n tr?ng
- **Button Primary**: N?n xanh (#2563EB), ch? tr?ng
- **Button Secondary**: N?n tr?ng, vi?n xám, ch? ?en
- **Badge**: Màu n?n rõ ràng v?i ch? t??ng ph?n

### Responsive Design
- Ho?t ??ng t?t trên m?i kích th??c màn hình
- Modal hi?n th? ??p trên mobile và desktop

## Các tính n?ng b? sung

### Validation
- Ki?m tra tên s?n ph?m không ???c r?ng
- Giá ph?i l?n h?n 0
- Kích th??c file ?nh không v??t quá 5MB
- ??nh d?ng file ?nh h?p l?

### Audit Log
- T? ??ng ghi log m?i thao tác CRUD
- L?u tr?:
  - Hành ??ng (CREATE/UPDATE/DELETE/SOFT_DELETE)
  - Giá tr? c? và m?i
  - User th?c hi?n
  - Th?i gian th?c hi?n

### Auto-generate Barcode
- T? ??ng t?o mã v?ch EAN-13 n?u không nh?p
- ??m b?o không trùng l?p
- ??nh d?ng: 890XXXXXXXXX (13 ch? s?)

## Database Schema

Các tr??ng trong b?ng `products`:
```sql
- product_id (INT, Primary Key)
- category_id (INT, Foreign Key)
- supplier_id (INT, Foreign Key)
- product_name (VARCHAR(100), Required)
- barcode (VARCHAR(50))
- price (DECIMAL(10,2), Required)
- cost_price (DECIMAL(10,2))
- unit (VARCHAR(20))
- status (VARCHAR(20))
- description (TEXT)
- image_url (VARCHAR(255))
- created_at (DATETIME)
- updated_at (DATETIME)
```

## H??ng d?n s? d?ng

1. **Truy c?p trang Products**:
   - ??ng nh?p v?i tài kho?n admin
   - Vào menu Admin > Products

2. **Thêm s?n ph?m ??u tiên**:
   - Click "Add Product"
   - ?i?n thông tin t?i thi?u: Tên và Giá
   - Upload hình ?nh (khuy?n khích)
   - Click "Create"

3. **Qu?n lý hình ?nh**:
   - Hình ?nh ???c l?u t? ??ng
   - Có th? thay ??i hình ?nh b?t c? lúc nào
   - Xóa hình ?nh b?ng nút X trên preview

4. **Xóa d? li?u mock**:
   - Mock data trong Program.cs ?ã ???c xóa
   - Database ch? còn các d? li?u kh?i t?o c?n thi?t:
     - Admin user (username: admin, password: admin123)
     - Sample categories
     - Main warehouse

## L?u ý k? thu?t

- Service ?ã ???c ??ng ký trong `Program.cs`
- S? d?ng transaction ?? ??m b?o tính toàn v?n d? li?u
- Repository pattern cho truy xu?t d? li?u
- Audit logging cho m?i thao tác quan tr?ng
