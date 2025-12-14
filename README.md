# Project Cua Hang - Hệ thống Quản lý Cửa hàng (Store Management)

Đây là một ứng dụng web quản lý cửa hàng bán lẻ và thương mại điện tử, được xây dựng bằng công nghệ **ASP.NET Core Blazor Interactive Server**. Dự án bao gồm các chức năng quản lý sản phẩm, đơn hàng, kho hàng, nhà cung cấp, và hệ thống xác thực người dùng tích hợp sẵn.

## 🚀 Công nghệ sử dụng

Dự án được xây dựng trên nền tảng .NET và các thư viện hiện đại:

* **Framework:** ASP.NET Core Blazor (Interactive Server Render Mode).
* **Ngôn ngữ:** C# (Target Framework: .NET 10.0 - *Lưu ý: Dựa trên file .csproj*).
* **Cơ sở dữ liệu:** SQLite (File: `store.db` cho dữ liệu nghiệp vụ, `app.db` cho Identity).
* **ORM:** Entity Framework Core.
* **UI Framework:** Blazorise (với Tailwind CSS provider).
* **Styling:** Tailwind CSS.
* **Authentication:** ASP.NET Core Identity (Cookie-based auth, hỗ trợ 2FA, Passkeys).

## 📂 Cấu trúc dự án

* **Components/**: Chứa các giao diện UI (Pages, Layouts, Shared components).
* **Models/**: Các thực thể cơ sở dữ liệu (Product, Order, User, v.v.).
* **DTOs/**: Data Transfer Objects dùng để truyền dữ liệu giữa các lớp.
* **Services/**: Chứa logic nghiệp vụ (OrderService, ProductService, AuthService, v.v.).
* **Repositories/**: Lớp truy cập dữ liệu (Generic Repository pattern).
* **Data/**: DbContext và cấu hình database.

## 🛠 Hướng dẫn Cài đặt và Chạy (Visual Studio)

Để chạy dự án này trên Visual Studio, hãy làm theo các bước sau:

### 1. Yêu cầu hệ thống
* Visual Studio 2022 (phiên bản mới nhất hỗ trợ .NET SDK tương ứng).
* .NET SDK (theo cấu hình trong `.csproj`).

### 2. Các bước thực hiện

1.  **Clone hoặc tải dự án** về máy.
2.  Mở file **`BlazorApp1.sln`** hoặc **`BlazorApp1.slnx`** bằng Visual Studio.
3.  **Restore NuGet Packages**:
    * Chuột phải vào Solution -> Chọn *Restore NuGet Packages*.
    * Đợi Visual Studio tải các thư viện cần thiết (Blazorise, EF Core, v.v.).
4.  **Cấu hình Database**:
    * Dự án sử dụng SQLite nên không cần cài đặt SQL Server.
    * Chuỗi kết nối mặc định trong `appsettings.json`: `Data Source=store.db` và `Data Source=app.db`.
    * Khi chạy ứng dụng lần đầu, `Program.cs` sẽ tự động gọi `storeContext.Database.EnsureCreated()` để tạo file database và thêm dữ liệu mẫu (Seed Data).
5.  **Chạy ứng dụng**:
    * Nhấn **F5** hoặc nút **Run** (https/http) trên thanh công cụ.

### 3. Tài khoản mặc định (Seed Data)
Nếu database được khởi tạo mới, hệ thống sẽ tạo các tài khoản mặc định:

* **Admin (Quản trị viên):**
    * **Email:** `admin@estore.com`
    * **Password:** `Admin123!`

* **Sales Staff (Nhân viên bán hàng):**
    * **Email:** `sales@estore.com`
    * **Password:** `Sales123!`

* **Warehouse Staff (Nhân viên kho):**
    * **Email:** `warehouse@estore.com`
    * **Password:** `Warehouse123!`

## 📖 Hướng dẫn xem Database (SQLite)

Dự án sử dụng 2 file database SQLite nằm trong thư mục gốc của dự án:
1.  **`store.db`**: Chứa dữ liệu nghiệp vụ (Sản phẩm, Đơn hàng, Khách hàng, v.v.).
2.  **`app.db`**: Chứa dữ liệu Identity (User, Role, Claim, Token).

Để xem và quản lý dữ liệu trong các file này, bạn có thể sử dụng các công cụ sau:

### 1. DB Browser for SQLite (Khuyên dùng)
Đây là công cụ miễn phí, nhẹ và dễ sử dụng.
*   **Tải về:** [https://sqlitebrowser.org/dl/](https://sqlitebrowser.org/dl/)
*   **Cách dùng:**
    1.  Mở phần mềm **DB Browser for SQLite**.
    2.  Chọn **Open Database**.
    3.  Trỏ đến file `store.db` hoặc `app.db` trong thư mục dự án.
    4.  Chuyển sang tab **Browse Data** để xem dữ liệu các bảng.

### 2. DBeaver Community
Công cụ quản lý database đa năng, hỗ trợ nhiều loại DB bao gồm SQLite.
*   **Tải về:** [https://dbeaver.io/download/](https://dbeaver.io/download/)
*   **Cách dùng:** Tạo kết nối mới -> Chọn SQLite -> Trỏ đến file `.db`.

### 3. Extension trong Visual Studio / VS Code
*   **Visual Studio:** Cài đặt extension **SQLite/SQL Server Compact Toolbox**.
*   **VS Code:** Cài đặt extension **SQLite** (alexcvzz) hoặc **SQLite Viewer**.

## 🔗 Tổng hợp các Route (Đường dẫn)

Dưới đây là danh sách các trang và đường dẫn truy cập trong hệ thống:

### 🏠 Public / Store (Cửa hàng)
| Chức năng | Route | Mô tả |
| :--- | :--- | :--- |
| **Trang chủ** | `/` | Trang giới thiệu, landing page. |
| **Sản phẩm** | `/store` | Danh sách sản phẩm, tìm kiếm, lọc. |
| **Danh mục** | `/store/categories` | Xem danh sách danh mục sản phẩm. |
| **Giỏ hàng** | `/store/cart` | Xem và quản lý giỏ hàng hiện tại. |
| **Thanh toán** | `/store/checkout` | Nhập thông tin giao hàng và đặt hàng. |
| **Đơn hàng của tôi**| `/store/orders` | Lịch sử đơn hàng của người dùng. |
| **Chi tiết đơn** | `/store/orders/{OrderId}` | Xem chi tiết một đơn hàng cụ thể. |
| **Hóa đơn** | `/store/orders/{OrderId}/bill` | Xem và in hóa đơn thanh toán. |

### 🔐 Authentication (Tài khoản)
| Chức năng | Route | Mô tả |
| :--- | :--- | :--- |
| **Đăng nhập** | `/Account/Login` | Đăng nhập hệ thống. |
| **Đăng xuất** | `/Account/Logout` | Đăng xuất khỏi hệ thống. |
| **Đăng ký** | `/Account/Register` | Tạo tài khoản mới. |
| **Quên mật khẩu** | `/Account/ForgotPassword` | Yêu cầu đặt lại mật khẩu. |
| **Xác thực Email** | `/Account/ConfirmEmail` | Link xác thực email. |
| **Hồ sơ** | `/Account/Manage` | Quản lý thông tin cá nhân. |
| **Đổi mật khẩu** | `/Account/Manage/ChangePassword`| Đổi mật khẩu đăng nhập. |
| **Bảo mật 2FA** | `/Account/Manage/TwoFactorAuthentication` | Cấu hình xác thực 2 lớp. |
| **Passkeys** | `/Account/Manage/Passkeys` | Quản lý đăng nhập không cần mật khẩu. |

### ⚠️ System (Hệ thống)
| Chức năng | Route | Mô tả |
| :--- | :--- | :--- |
| **Lỗi** | `/Error` | Trang hiển thị khi có lỗi hệ thống. |
| **Không tìm thấy**| `/not-found` | Trang 404. |
| **Từ chối truy cập**| `/Account/AccessDenied` | Trang 403 khi không có quyền hạn. |
| **Bị khóa** | `/Account/Lockout` | Khi tài khoản bị khóa tạm thời. |

## 📝 Tính năng nổi bật

1.  **Quản lý tồn kho tự động:** Khi tạo đơn nhập (`PurchaseOrder`), tồn kho tự động tăng. Khi bán hàng (`Order`), tồn kho tự động giảm theo logic FIFO tại các kho.
2.  **Audit Logging:** Hệ thống ghi lại mọi thao tác quan trọng (Login, Create Order, Update Product...) vào bảng `AuditLogs`.
3.  **Khuyến mãi:** Hỗ trợ mã giảm giá (Promotion) theo phần trăm, số tiền cố định, áp dụng cho đơn hàng hoặc sản phẩm cụ thể.
4.  **Báo cáo thống kê:** API hỗ trợ thống kê doanh thu theo ngày, tháng, năm và các sản phẩm bán chạy.

---
*Copyright © 2025 DotnetTeam*
