# SPECIFICATION.md
# Đặc tả yêu cầu phần mềm — OS Memory Allocation Simulator

> **Trạng thái:** Bản tổng hợp yêu cầu để nhóm thống nhất trước khi triển khai.  
> Tài liệu này tổng hợp yêu cầu hệ thống. Hợp đồng interface giữa module và schema CSV chi tiết sẽ được chốt trong `docs/architecture/module-contracts-and-csv-schema.md`. Khi có điểm chưa rõ hoặc xung đột giữa tài liệu, nhóm cần ghi nhận và thống nhất trước khi triển khai, không tự suy đoán.

## 1. Mục đích

Xây dựng ứng dụng desktop có GUI để mô phỏng cấp phát/thu hồi bộ nhớ liên tục bằng First-Fit, Best-Fit và Worst-Fit; trực quan hóa Memory Map và Trace; đánh giá ba thuật toán trên cùng cấu hình/workload; xuất dữ liệu chi tiết và kết quả so sánh ra CSV để kiểm tra và tái lập.

Ứng dụng phục vụ học tập, **không trực tiếp quản lý RAM vật lý của máy tính**.

## 2. Phạm vi

### 2.1. Trong phạm vi
- Cấu hình dung lượng bộ nhớ và đơn vị cấp phát.
- Đọc, kiểm tra cấu hình/workload CSV.
- Mô phỏng `ALLOCATE` và `FREE` bằng một thuật toán được chọn.
- Hiển thị Memory Map, Trace và chỉ số bộ nhớ.
- Đánh giá độc lập cả ba thuật toán trên cùng cấu hình và workload.
- Hiển thị bảng so sánh, bốn biểu đồ và nhận xét dựa trên số liệu.
- Xuất Trace CSV và Comparison CSV, xử lý lỗi ghi file.
- Unit/integration tests, kiểm thử trường hợp biên, stress/performance tests và tài liệu bàn giao.

### 2.2. Ngoài phạm vi
- Quản lý hoặc thay đổi RAM vật lý của hệ điều hành.
- Paging, segmentation hoặc thuật toán quản lý bộ nhớ khác ngoài phạm vi đề tài.
- Tự thay đổi workload để ưu tiên một thuật toán.
- Khẳng định một thuật toán luôn tốt nhất trong mọi trường hợp.

## 3. Thuật ngữ

| Thuật ngữ | Ý nghĩa |
|---|---|
| Memory block | Đoạn nhớ có địa chỉ bắt đầu, kích thước và trạng thái. |
| Free block / hole | Đoạn nhớ chưa được cấp phát. |
| PID / ProcessId | Mã định danh tiến trình trong workload. |
| Workload | Chuỗi thao tác `ALLOCATE` và `FREE`, xử lý theo `Sequence` tăng dần. |
| Memory Map | Biểu diễn các đoạn nhớ theo địa chỉ, kích thước và trạng thái. |
| Trace | Nhật ký kết quả sau từng thao tác workload. |
| Internal fragmentation | Phần chênh lệch giữa kích thước thực tế cấp phát và kích thước yêu cầu ban đầu của tiến trình còn đang chiếm bộ nhớ. |
| External fragmentation | Bộ nhớ trống bị chia thành nhiều đoạn rời rạc, không có đoạn đơn lẻ đủ lớn cho yêu cầu. |
| Simulation | Luồng người dùng mô phỏng một thuật toán được chọn. |
| Evaluation | Luồng người dùng chạy và so sánh cả ba thuật toán trên cùng điều kiện đầu vào. |

## 4. Mô hình và quy tắc nghiệp vụ

### 4.1. Mô hình bộ nhớ
- Địa chỉ bắt đầu từ `0`; đơn vị kích thước là KB.
- Ban đầu toàn bộ bộ nhớ là một đoạn trống có kích thước `MemorySize`.
- Mỗi đoạn có `StartAddress`, `Size`, trạng thái trống/đã cấp phát và thông tin PID/kích thước yêu cầu ban đầu nếu được cấp phát.
- Các đoạn được sắp xếp theo địa chỉ tăng dần, không chồng lấn, không để hở địa chỉ.
- Sau mọi thao tác, tổng kích thước các đoạn phải bằng `MemorySize`; `UsedMemory + FreeMemory = MemorySize`.

### 4.2. Cấu hình
- `MemorySize` và `AllocationUnit` là số nguyên dương, đơn vị KB.
- `MemorySize` không bắt buộc chia hết cho `AllocationUnit`.
- Cấu hình có thể được nhập trực tiếp hoặc đọc từ `Config.csv`; một lượt chạy chỉ dùng một nguồn cấu hình.
- Giá trị mặc định cụ thể nếu có phải được thống nhất trong hợp đồng cấu hình.

### 4.3. Làm tròn kích thước
Với yêu cầu ban đầu `R` và đơn vị cấp phát `U`:

`AllocatedSize = ceil(R / U) * U`

- `R` phải là số nguyên dương với `ALLOCATE`.
- Nếu kích thước sau làm tròn lớn hơn `MemorySize`, thao tác thất bại với `REQUEST_EXCEEDS_MEMORY`.
- Nếu không có đoạn trống liên tục đủ lớn, thao tác thất bại với `INSUFFICIENT_MEMORY`.
- Đoạn trống nhỏ hơn `AllocationUnit` vẫn thuộc `FreeMemory`, nhưng không đủ điều kiện đáp ứng một yêu cầu sau làm tròn nếu kích thước đoạn nhỏ hơn kích thước cần cấp.

### 4.4. Ba thuật toán
- **First-Fit:** Chọn đoạn trống đầu tiên đủ lớn khi duyệt theo địa chỉ tăng dần.
- **Best-Fit:** Chọn đoạn trống đủ lớn nhỏ nhất.
- **Worst-Fit:** Chọn đoạn trống đủ lớn lớn nhất.
- **Tie-breaking:** Khi nhiều đoạn cùng thỏa tiêu chí, ưu tiên đoạn có địa chỉ bắt đầu nhỏ hơn.

Khi cấp phát thành công, phần đầu của đoạn trống được cấp cho PID. Nếu đoạn lớn hơn kích thước cấp phát, phần dư được giữ thành đoạn trống liền sau. Các quy tắc tách đoạn, cập nhật trạng thái và ghi Trace phải nhất quán giữa ba thuật toán.

### 4.5. PID và thu hồi
- Một PID không được có nhiều hơn một đoạn nhớ đang cấp phát.
- `ALLOCATE` cho PID đang hoạt động thất bại với `DUPLICATE_ACTIVE_PID`.
- `FREE` chỉ thành công khi PID hiện sở hữu một đoạn nhớ.
- PID không tồn tại/đã được thu hồi dẫn đến `PID_NOT_FOUND`.
- Khi thu hồi thành công, đoạn nhớ trở thành trống; gộp với đoạn trống liền kề bên trái, bên phải hoặc cả hai phía.
- Thao tác thất bại không thay đổi trạng thái bộ nhớ; mô phỏng tiếp tục với thao tác kế tiếp.
- Thất bại nghiệp vụ trong workload hợp lệ không phải lỗi CSV và không làm hủy toàn bộ lượt chạy.

### 4.6. Bất biến sau thao tác
- Tổng kích thước mọi đoạn bằng `MemorySize`.
- Không chồng lấn, không hở địa chỉ, sắp xếp theo địa chỉ tăng dần.
- Kích thước mỗi đoạn dương.
- Mỗi PID đang hoạt động gắn với đúng một đoạn đã cấp phát.
- Kích thước đã cấp phát không nhỏ hơn yêu cầu ban đầu.
- Sau `FREE`, không còn hai đoạn trống liền kề chưa được gộp.

## 5. Yêu cầu chức năng

**Simulation và Evaluation là hai luồng người dùng độc lập.** Chúng có thể dùng chung mô hình, thuật toán, CSV reader/validator và phép tính chỉ số, nhưng Evaluation không yêu cầu người dùng mở hoặc chạy màn hình Simulation trước.

### 5.1. Simulation — Mô phỏng

#### SIM-01: Cấu hình
Người dùng có thể nhập `MemorySize`/`AllocationUnit` trực tiếp hoặc đọc `Config.csv`; chọn workload mẫu hoặc workload CSV; xem lỗi cấu hình/dữ liệu; chỉ bắt đầu khi cấu hình và workload hợp lệ.

#### SIM-02: Chạy mô phỏng
- Chọn đúng một thuật toán: First-Fit, Best-Fit hoặc Worst-Fit.
- Khởi tạo trạng thái bộ nhớ trống mới.
- Sắp xếp workload theo `Sequence` tăng dần rồi xử lý từng thao tác.
- Hỗ trợ các thao tác chạy Step/Reset theo hành vi đã thống nhất trong mockup và hợp đồng UI.
- Reset phải tạo trạng thái mới, không giữ lại cấp phát từ lượt trước.

#### SIM-03: Memory Map và Trace
Sau mỗi thao tác:
- Cập nhật Memory Map theo địa chỉ tăng dần.
- Hiển thị địa chỉ bắt đầu, kích thước, trạng thái và PID nếu đã cấp phát.
- Ghi đúng một Trace entry cho thao tác vừa xử lý, kể cả thao tác thất bại.
- Ghi `SUCCESS`/`FAILED`, mã nguyên nhân và các chỉ số sau thao tác.
- Hiển thị tối thiểu `UsedMemory`, `FreeMemory`, `FreeBlockCount`, `LargestFreeBlock`, `TotalInternalFragmentation`.

#### SIM-04: Xuất Trace
- Xuất Trace của thuật toán đang mô phỏng thành CSV.
- CSV phải khớp Trace trên giao diện và giữ đúng thứ tự thao tác.
- Có header và định dạng nhất quán.
- Nếu file đích tồn tại, cung cấp lựa chọn ghi đè, đổi tên/chọn vị trí khác hoặc hủy.
- Nếu ghi file thất bại, báo lỗi, không báo thành công giả và không thay đổi kết quả mô phỏng.

### 5.2. Evaluation — Đánh giá & báo cáo

#### EVAL-01: Cấu hình
- Có màn hình cấu hình riêng, tương tự cấu hình Simulation.
- Nhập trực tiếp hoặc nạp `Config.csv`; chọn workload mẫu hoặc workload CSV.
- Chỉ chạy khi toàn bộ cấu hình/workload hợp lệ.
- Có thể chuyển qua lại giữa Simulation và Evaluation mà không bắt buộc chạy chức năng còn lại.

#### EVAL-02: Chạy benchmark
- Chạy cả ba thuật toán trên cùng cấu hình, workload và trạng thái bộ nhớ ban đầu.
- Mỗi thuật toán có trạng thái độc lập; không dùng trạng thái cuối của thuật toán này làm trạng thái đầu cho thuật toán khác.
- Thu thập kết quả của từng thuật toán, bao gồm kết quả cấp phát, Trace, trạng thái bộ nhớ cuối và các chỉ số cần thiết cho bảng so sánh.
- Đo thời gian riêng cho từng thuật toán, 5 lần trên cùng workload và báo cáo trung bình theo đặc tả/mockup.
- Đo thời gian không được làm thay đổi dữ liệu dùng để tính các chỉ số mô phỏng.
- Khi hủy lúc đang chạy, không được hiển thị kết quả hoàn tất giả; trạng thái UI theo mockup.

#### EVAL-03: Bảng và biểu đồ
Bảng so sánh có một hàng cho mỗi thuật toán và tối thiểu gồm:

**Kết quả cấp phát**
- Tổng số yêu cầu `ALLOCATE`.
- Số lần thành công và thất bại.
- Tỷ lệ thành công.

**Bộ nhớ tại cuối workload**
- Tổng bộ nhớ sử dụng.
- Tổng bộ nhớ trống.
- Số vùng trống.
- Kích thước vùng trống lớn nhất.
- Tổng phân mảnh nội của các tiến trình còn đang chiếm bộ nhớ.

**Hiệu năng**
- Thời gian chạy trung bình của 5 lần đo.

Bốn biểu đồ:
1. Số lần cấp phát thành công và thất bại.
2. Tổng phân mảnh nội.
3. Kích thước vùng trống lớn nhất.
4. Thời gian chạy.

Nhận xét phải dựa trên số liệu thực tế, không khẳng định thuật toán nào luôn tốt nhất. Nếu workload không có `ALLOCATE`, tỷ lệ thành công không xác định: UI hiển thị `N/A`, trường `SuccessRate` trong Comparison CSV để trống.

#### EVAL-04: Xuất báo cáo
- Xuất ba Trace CSV riêng, mỗi thuật toán một file.
- Xuất một Comparison CSV có dữ liệu cả ba thuật toán.
- Dữ liệu xuất phải khớp với Trace/bảng kết quả đang hiển thị.
- Tên file nhận diện được thuật toán/loại báo cáo, workload và thời điểm xuất; quy tắc chính xác nằm trong hợp đồng CSV.
- Khi xuất nhiều file, phải báo trạng thái từng file. Lỗi một file không được làm mất kết quả đánh giá hoặc ảnh hưởng file đã xuất thành công.

## 6. Yêu cầu CSV

Schema chi tiết được quản lý trong `docs/architecture/module-contracts-and-csv-schema.md`. Các quy tắc hiện đã xác định như sau.

### 6.1. Config CSV

Header chính xác:

```csv
MemorySize,AllocationUnit
```

Chỉ một bản ghi; cả hai giá trị là số nguyên dương theo KB.

```csv
MemorySize,AllocationUnit
1000,64
```

### 6.2. Workload CSV

Header chính xác:

```csv
Sequence,Operation,ProcessId,RequestedSize
```

- `Sequence`: số nguyên dương, không trùng; không cần bắt đầu từ 1 hoặc liên tục.
- `Operation`: chỉ `ALLOCATE` hoặc `FREE`.
- `ProcessId`: không rỗng, tuân theo quy tắc PID được thống nhất.
- `RequestedSize`: số nguyên dương với `ALLOCATE`; với `FREE`, trường này không dùng để xác định kích thước thu hồi và có thể để trống.
- Sắp xếp thao tác theo `Sequence` tăng dần trước khi chạy.

Ví dụ:

```csv
Sequence,Operation,ProcessId,RequestedSize
1,ALLOCATE,P1,100
2,ALLOCATE,P2,200
3,FREE,P1,
4,ALLOCATE,P3,65
5,FREE,P2,
```

### 6.3. Quy tắc đọc CSV
- Dùng dấu phẩy, không tự chuyển dấu chấm phẩy.
- Header phải khớp chính xác tên và thứ tự cột.
- Giá trị số là số nguyên, không kèm đơn vị.
- Cắt khoảng trắng đầu/cuối mỗi ô; bỏ qua dòng hoàn toàn trống.
- Hỗ trợ quy tắc CSV thông thường, gồm trường đặt trong dấu ngoặc kép khi cần.
- Kiểm tra toàn bộ file trước khi chạy; nếu có lỗi, từ chối toàn bộ workload, không chạy một phần.
- Thông báo lỗi cần chỉ ra dòng, trường và nguyên nhân nếu xác định được.
- Phân biệt lỗi dữ liệu đầu vào với lỗi nghiệp vụ trong workload hợp lệ.

### 6.4. Trace CSV

Header chính thức:

```csv
Sequence,Operation,ProcessId,RequestedSize,AllocatedSize,Status,Reason,TotalFreeMemory,LargestFreeBlock,InternalFragmentation
```

- Mỗi thao tác đã xử lý tạo đúng một dòng, kể cả thất bại.
- Các chỉ số ghi trạng thái **sau** thao tác.
- `Status`: `SUCCESS` hoặc `FAILED`.
- `Reason` dùng mã cố định.
- `ALLOCATE` thành công: `AllocatedSize` là kích thước sau làm tròn.
- `ALLOCATE` thất bại: `AllocatedSize` để trống.
- `FREE` thành công: `AllocatedSize` là kích thước vùng vừa thu hồi trước khi gộp.
- `FREE` thất bại: `AllocatedSize` để trống.
- Lỗi CSV trước khi chạy không tạo Trace entry.

Các mã nghiệp vụ theo đặc tả:
- `SUCCESS`
- `INSUFFICIENT_MEMORY`
- `REQUEST_EXCEEDS_MEMORY`
- `DUPLICATE_ACTIVE_PID`
- `PID_NOT_FOUND`

### 6.5. Comparison CSV

Header chính thức:

```csv
WorkloadName,Algorithm,OperationCount,MemorySize,AllocationUnit,TotalAllocationRequests,SuccessfulAllocations,FailedAllocations,SuccessRate,UsedMemory,FreeMemory,FreeBlockCount,LargestFreeBlock,TotalInternalFragmentation,Five runtime measurements,AverageRuntimeMs
```

- Chứa một bản ghi tổng hợp.
- Kết quả phải dùng cùng cấu hình/workload.
- `SuccessRate` để trống khi không có yêu cầu `ALLOCATE`.

## 7. Yêu cầu giao diện
- Ứng dụng desktop có GUI chạy trên Windows; không phải console app hoặc web app.
- Có hai chức năng cấp cao: `Mô phỏng` và `Đánh giá & Báo cáo`.
- Mỗi chức năng có cấu hình và nút chạy riêng.
- Phân biệt trạng thái chưa có kết quả, dữ liệu không hợp lệ, đang chạy, hoàn tất và lỗi thao tác.
- Nút chạy chỉ bật khi cấu hình và workload hợp lệ.
- Hiển thị lỗi rõ ràng; không crash với dữ liệu hợp lệ hoặc lỗi đầu vào có thể dự đoán.
- Bám theo mockup đã thống nhất về bố cục, nhãn và hành vi nút.

## 8. Yêu cầu phi chức năng

- **Tính đúng đắn:** đối chiếu kết quả với pseudocode, giáo trình hoặc test oracle độc lập với GUI.
- **Tách trách nhiệm:** thuật toán/mô hình bộ nhớ không đặt trong code-behind; Simulation và Evaluation độc lập ở cấp luồng người dùng, có thể dùng chung lõi nghiệp vụ.
- **Tái lập:** lưu/giữ workload, cấu hình, kết quả và phương pháp đo để có thể chạy lại.
- **Xử lý lỗi:** kiểm tra trước khi chạy, thông báo rõ, không báo xuất file thành công giả.
- **Hiệu năng:** benchmark chuẩn gồm 100, 1.000 và 10.000 thao tác; đây là các mức benchmark, không phải giới hạn tối đa. Báo cáo số liệu, phương pháp đo, độ phức tạp và hạn chế.
- **Khả năng kiểm thử:** unit test cho thuật toán, mô hình bộ nhớ, CSV; integration test cho cả hai luồng.
- **Khả năng bảo trì:** trách nhiệm module và schema dữ liệu phải rõ, được thống nhất và cập nhật có kiểm soát.

## 9. Kiểm thử và nghiệm thu

### 9.1. Thuật toán/mô hình
Kiểm thử khởi tạo; lựa chọn của ba thuật toán; tie-breaking; cấp phát vừa khít/tách phần dư; làm tròn kích thước; yêu cầu bằng hoặc vượt dung lượng bộ nhớ; phân mảnh ngoại; PID trùng; FREE PID có/không tồn tại; gộp trái/phải/cả hai phía; và bất biến bộ nhớ sau thành công/thất bại.

### 9.2. CSV
Kiểm thử header sai/thiếu cột/sai thứ tự; sai số lượng trường; số sai định dạng hoặc không dương; Sequence trùng; Operation không hợp lệ; thiếu RequestedSize với ALLOCATE; dòng trống/khoảng trắng; và workload hợp lệ có thao tác thất bại nghiệp vụ.

### 9.3. Tích hợp
- Simulation: cấu hình → workload → chạy → Memory Map/Trace → xuất Trace CSV.
- Evaluation: cấu hình → workload → chạy ba thuật toán độc lập → bảng/biểu đồ → ba Trace CSV và Comparison CSV.
- Xác minh CSV xuất khớp kết quả UI.
- Kiểm tra lỗi xuất một file không làm mất kết quả đánh giá.
- Stress/performance test với 100, 1.000 và 10.000 thao tác.

### 9.4. Điều kiện nghiệm thu
- Chạy được trên Windows theo README.
- Có expected/actual result cho các test đại diện, đối chiếu bằng test oracle.
- Hai luồng Simulation/Evaluation hoạt động độc lập và đúng.
- Memory Map, Trace, bảng, biểu đồ và CSV phản ánh cùng kết quả tính toán.
- Có xử lý lỗi dữ liệu và lỗi xuất file.
- Có unit/integration tests, báo cáo kiểm thử/hiệu năng, CSV mẫu, báo cáo đồ án và video demo theo yêu cầu giảng viên.
- Báo cáo có contribution statement và bảng phần việc từng thành viên.

## 10. Sản phẩm bàn giao
- Git repository và mã nguồn.
- Ứng dụng desktop chạy được.
- README cài đặt/chạy/sử dụng.
- Thư mục input/output với CSV mẫu và dữ liệu cần thiết để tái lập.
- Đặc tả, sơ đồ/pseudocode/flowchart.
- Hợp đồng module và schema CSV.
- Bảng kiểm thử expected/actual.
- Báo cáo hiệu năng, hạn chế, báo cáo đồ án và video demo công khai.

## 11. Tài liệu liên quan
- `docs/architecture/module-contracts-and-csv-schema.md` — hợp đồng module, kiểu dữ liệu trao đổi, schema CSV và lỗi.
- `docs/architecture/mvvm-conventions.md` — quy ước WPF/MVVM.
- `README.md` — hướng dẫn chạy và ví dụ sử dụng.
- Mockup cấu hình mô phỏng, mô phỏng bộ nhớ và đánh giá/báo cáo — căn cứ giao diện.
