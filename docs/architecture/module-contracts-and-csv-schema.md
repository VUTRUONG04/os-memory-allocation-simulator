# Hợp đồng Module và Schema CSV

> **Dự án:** OS Memory Allocation Simulator — Chủ đề 11  
> **Trạng thái:** Hợp đồng triển khai dựa trên `SPECIFICATION.md` và các quyết định đã thống nhất của nhóm.
> **Phạm vi:** Trách nhiệm các module, hợp đồng dữ liệu, xác thực/xuất CSV, các chỉ số, đo thời gian chạy, và vòng đời kết quả.

## 1. Thẩm quyền và cách giải thích

Tài liệu này là phần bổ trợ cho `SPECIFICATION.md`, không thay thế nó.

Quy tắc được đánh dấu:

- **[SPEC]** — được nêu rõ trong `SPECIFICATION.md` hoặc đặc tả dự án.
- **[TEAM]** — quyết định đã được nhóm thống nhất.
- **[DESIGN]** — đề xuất triển khai để các module tương tác được. Không được mâu thuẫn với `[SPEC]` hoặc `[TEAM]`.

Nếu tài liệu này và `SPECIFICATION.md` xung đột, hãy tuân theo `SPECIFICATION.md` và cập nhật tài liệu này. Không tự tạo quy tắc mới.

## 2. Ranh giới kiến trúc

### 2.1 Hướng phụ thuộc

```
View (XAML)
  → ViewModel (binding, commands, UI state/orchestration)
      → Dịch vụ ứng dụng (Application Services)
          → Input/Output và domain services
          → Thuật toán cấp phát
          → Mô hình bộ nhớ
```

- **[TEAM] View:** bố cục XAML và binding. Không chứa quy tắc nghiệp vụ.
- **[TEAM] ViewModel:** trạng thái UI, command, validation, trạng thái tải/ chạy/ lỗi, và phối hợp các service. Áp dụng quy tắc MVVM của dự án, bao gồm `ObservableObject`, `[ObservableProperty]`, và `[RelayCommand]`.
- **[DESIGN] Dịch vụ ứng dụng:** điều phối các use‑case như mô phỏng, đánh giá, CSV import/export, và vô hiệu hoá kết quả.
- **[TEAM] Thuật toán / lõi domain:** chịu trách nhiệm quyết định cấp phát và chuyển trạng thái bộ nhớ. Không phụ thuộc vào WPF hay ViewModel.
- **[DESIGN] Dịch vụ CSV:** đọc và xác thực toàn bộ file đầu vào trước khi chạy; serialize DTO theo schema chuẩn trong tài liệu này.

### 2.2 Luồng người dùng độc lập

**[SPEC][TEAM]** Simulation và Evaluation là hai luồng người dùng độc lập. Chúng có thể chia sẻ mô hình domain, thuật toán, validation workload, tính chỉ số, và mã CSV, nhưng Evaluation không cần mở hoặc chạy màn hình Simulation trước.

- **Simulation:** chọn một thuật toán, chạy một lượt, hiển thị Memory Map và Trace hiện tại, các chỉ số, và xuất một Trace CSV.
- **Evaluation:** chạy cả ba thuật toán trên cùng một cấu hình và workload; mỗi thuật toán bắt đầu từ trạng thái bộ nhớ mới; hiển thị bảng so sánh, biểu đồ, báo cáo; xuất ba Trace CSV và một Comparison CSV.

ViewModel của một luồng không được dùng kết quả của luồng còn lại làm nguồn dữ liệu.

## 3. Hợp đồng domain chuẩn

### 3.1 Cấu hình

```csharp
public sealed record SimulationConfig(
    int MemorySize,
    int AllocationUnit);
```

Quy tắc xác thực:
- `MemorySize > 0` và `AllocationUnit > 0`.
- **[TEAM]** `MemorySize >= AllocationUnit` là bắt buộc.
- **[TEAM]** `MemorySize` không cần chia hết cho `AllocationUnit` (ví dụ 103 KB với đơn vị 10 KB).
- **[DESIGN]** Từ chối cấu hình không hợp lệ trước khi tạo lượt chạy.

### 3.2 Workload và thao tác

```csharp
public enum MemoryOperationType
{
    Allocate,
    Free
}

public sealed record WorkloadOperation(
    int Sequence,
    MemoryOperationType Operation,
    string ProcessId,
    int? RequestedSize);
```

Hợp đồng:
- `Sequence` là số nguyên dương và độc nhất trong workload.
- **[TEAM]** Xác thực toàn bộ hàng trước, sau đó xử lý theo `Sequence` tăng dần; dữ liệu không cần sắp xếp trước.
- `ProcessId` được trim cả hai đầu, phân biệt hoa/thường; rỗng → không hợp lệ.
- Với `ALLOCATE`, `RequestedSize` phải là số nguyên dương.
- Với `FREE`, `RequestedSize` không được dùng, có thể để trống/null.
- Mỗi PID chỉ có một vùng nhớ đang cấp phát; được phép tái sử dụng sau khi `FREE` thành công.

### 3.3 Vùng nhớ và trạng thái bộ nhớ

```csharp
public sealed record MemoryRegion(
    int StartAddress,
    int Size,
    bool IsFree,
    string? ProcessId,
    int? RequestedSize);

public sealed class MemoryState
{
    public int MemorySize { get; }
    public IReadOnlyList<MemoryRegion> Regions { get; }
}
```

Các bất biến cần bảo đảm:
1. Các region được sắp xếp tăng dần `StartAddress`.
2. Region đầu tiên bắt đầu ở địa chỉ `0`.
3. Mỗi region bao phủ địa chỉ `S` tới `S+Z-1`; region tiếp theo bắt đầu tại `S+Z`.
4. `Size > 0` cho mọi region; không chồng lấn, không có khoảng trống địa chỉ.
5. Tổng kích thước các region bằng `MemorySize`.
6. Region `free` không có PID; region `allocated` có PID và kích thước yêu cầu ban đầu.
7. Mỗi PID hoạt động xác định đúng một region allocated.
8. Sau `FREE` thành công, không để lại hai region `free` liền kề.
9. Thao tác thất bại không thay đổi trạng thái bộ nhớ.

### 3.4 Thuật toán cấp phát

**[SPEC]** Tất cả thuật toán nhận trạng thái ban đầu đồng nhất và cùng chuỗi thao tác.

- **First‑Fit:** duyệt region `free` theo địa chỉ tăng dần, chọn region đầu đủ lớn.
- **Best‑Fit:** chọn region `free` nhỏ nhất đủ lớn.
- **Worst‑Fit:** chọn region `free` lớn nhất đủ lớn.
- **Tie‑breaking:** nếu có nhiều region đáp ứng, chọn có `StartAddress` thấp nhất.
- Kích thước yêu cầu `R` được làm tròn: `A = ceil(R / U) * U` (U = AllocationUnit).
- Khi cấp phát thành công, cấp phát từ đầu region; nếu region lớn hơn `A` thì tách phần dư thành region `free` mới.
- Khi `FREE` thành công, đánh dấu region `free` và gộp với các region `free` liền kề.
- Thao tác thất bại giữ nguyên trạng thái và tiếp tục.

### 3.5 Kết quả thao tác và mục Trace

```csharp
public enum OperationStatus
{
    Success,
    Failed
}

public sealed record TraceEntry(
    int Sequence,
    MemoryOperationType Operation,
    string ProcessId,
    int? RequestedSize,
    int? AllocatedSize,
    OperationStatus Status,
    string Reason,
    int TotalFreeMemory,
    int LargestFreeBlock,
    int InternalFragmentation);
```

- `AllocatedSize` nullable: cho `ALLOCATE` thành công là kích thước đã làm tròn; cho thất bại là null. Đối với `FREE` thành công là kích thước vùng vừa thu hồi **trước** khi gộp; thất bại là null.
- `Status` xuất ra CSV là `SUCCESS` hoặc `FAILED`.
- `Reason` là `SUCCESS` hoặc một trong các mã lỗi dưới mục 3.6.
- Các chỉ số được đo **sau** thao tác; đối với thao tác thất bại chúng bằng giá trị trước thao tác.
- **[TEAM]** `InternalFragmentation` là tổng fragmentation nội bộ của **tất cả** các allocation hiện có, không chỉ của tiến trình đang thao tác.

### 3.6 Mã lỗi thất bại

| Mã | Ý nghĩa | Thay đổi trạng thái? |
|---|---|---:|
| `SUCCESS` | Thao tác thành công | Có (nơi áp dụng) |
| `REQUEST_EXCEEDS_MEMORY` | Kích thước đã làm tròn lớn hơn `MemorySize` | Không |
| `INSUFFICIENT_MEMORY` | Không có region `free` đủ lớn | Không |
| `DUPLICATE_ACTIVE_PID` | `ALLOCATE` dùng PID đã đang cấp phát | Không |
| `PID_NOT_FOUND` | `FREE` dùng PID không tồn tại hoặc đã được giải phóng | Không |

**[SPEC][TEAM]** Các lỗi trên là lỗi nghiệp vụ; ghi vào Trace và tiếp tục. Lỗi đầu vào CSV/ cấu hình ngăn chạy và không tạo Trace.

### 3.7 Snapshot chỉ số

```csharp
public sealed record MemoryMetrics(
    int UsedMemory,
    int FreeMemory,
    int FreeBlockCount,
    int LargestFreeBlock,
    int TotalInternalFragmentation);
```

- `UsedMemory` = tổng kích thước các region allocated.
- `FreeMemory` = tổng kích thước các region free (bao gồm tail nhỏ hơn AllocationUnit).
- `FreeBlockCount` = số region free.
- `LargestFreeBlock` = kích thước lớn nhất của region free (0 nếu không có).
- `TotalInternalFragmentation` = tổng `(AllocatedSize - RequestedSize)` của các allocation hiện có.
- Luôn phải có `UsedMemory + FreeMemory == MemorySize`.

## 4. Hợp đồng Service

### 4.1 Xác thực và đọc workload
```csharp
public interface IConfigValidator
{
    ValidationResult<SimulationConfig> Validate(SimulationConfig candidate);
}

public interface IWorkloadReader
{
    WorkloadLoadResult ReadAndValidate(string filePath);
}
```
- Phân tích toàn bộ file, xác thực header, kiểu, giá trị, và ràng buộc duy nhất trước khi trả về workload có thể chạy.
- Trim whitespace, chuẩn hoá PID theo quy tắc đội.
- Bỏ qua dòng trống hoàn toàn; không im lặng các dòng lỗi.
- Sắp xếp các operation theo `Sequence` tăng dần trước khi trả về.
- Trả về `WorkloadLoadResult` chỉ có workload hợp lệ hoặc danh sách lỗi; không trả về workload một phần.

### 4.2 Service mô phỏng / chạy thuật toán
```csharp
public interface ISimulationService
{
    SimulationResult Run(
        SimulationConfig config,
        IReadOnlyList<WorkloadOperation> workload,
        AllocationAlgorithmType algorithm);
}
```
- Tạo trạng thái bộ nhớ khởi tạo một region free từ `0` kích thước `MemorySize`.
- Xử lý workload đã chuẩn hoá theo `Sequence` tăng dần.
- Áp dụng thuật toán được chọn, cập nhật trạng thái bộ nhớ.
- Tạo một Trace entry cho mỗi operation.
- Trả về snapshot cuối cùng, các chỉ số cuối, và toàn bộ Trace.
- Khi operation thất bại, giữ nguyên trạng thái và tiếp tục.

### 4.3 Service đánh giá (Evaluation)
```csharp
public interface IEvaluationService
{
    EvaluationResult Run(
        SimulationConfig config,
        IReadOnlyList<WorkloadOperation> workload);
}
```
- Chạy First‑Fit, Best‑Fit, Worst‑Fit trên cùng cấu hình và workload.
- Mỗi thuật toán có trạng thái bộ nhớ mới.
- Giữ Trace và kết quả cuối riêng cho mỗi thuật toán.
- Đo thời gian chạy 5 lần cho mỗi thuật toán và tính trung bình.
- Nếu hủy hoặc lỗi, không hiện kết quả giả; thông báo trạng thái lỗi cho ViewModel.

### 4.4 Dịch vụ xuất CSV
```csharp
public interface ITraceCsvExporter
{
    ExportResult Export(TraceExportRequest request);
}

public interface IComparisonCsvExporter
{
    ExportResult Export(ComparisonExportRequest request);
}
```
- Chỉ serialize các schema chuẩn ở mục 5.
- Xử lý lỗi I/O như không ghi được file; không coi là lỗi mô phỏng.
- Không báo thành công cho tới khi ghi file thực sự thành công.
- Khi file đã tồn tại, UI phải cung cấp tùy chọn ghi đè, đổi tên, hoặc hủy.
- Lỗi xuất một file không làm mất kết quả đã xuất thành công các file khác.

### 4.5 Tích hợp ViewModel và vô hiệu hoá kết quả
- **[TEAM]** Simulation và Evaluation mỗi luồng có cấu hình, workload, trạng thái và kết quả riêng.
- ViewModel chỉ gọi service qua command; không chứa thuật toán hay logic CSV.
- `ObservableCollection<TraceEntry>` hoặc collection bất biến dùng để binding.
- `ObservableCollection<T>` thông báo khi collection thay đổi, nhưng không tự động thông báo khi thuộc tính bên trong item thay đổi; sử dụng DTO bất biến hoặc thông báo riêng.
- Các trạng thái chạy, hoàn thành, lỗi, hủy phải phân biệt rõ.
- **[TEAM]** Khi cấu hình hoặc workload thực tế thay đổi, vô hiệu hoá (xóa) kết quả cũ của luồng tương ứng. Không hiển thị kết quả cũ cho input mới.

## 5. Schema CSV

### 5.1 Config CSV
Header và thứ tự **chính xác**:
```
MemorySize,AllocationUnit
```
- Ghi đúng một bản ghi.
- Hai trường là số nguyên dương (KB).
- Áp dụng quy tắc xác thực ở mục 3.1.

### 5.2 Workload CSV
Header và thứ tự **chính xác**:
```
Sequence,Operation,ProcessId,RequestedSize
```
Quy tắc:
- `Sequence` số nguyên dương, duy nhất; xử lý theo tăng dần.
- `Operation` phải là `ALLOCATE` hoặc `FREE` (không có khoảng trắng dư).
- `ProcessId` không rỗng sau trim, phân biệt hoa/thường.
- `RequestedSize` số nguyên dương cho `ALLOCATE`; để trống cho `FREE`.
- Không có đơn vị trong cột số.
- Duplicate `Sequence` làm toàn bộ file không hợp lệ.

### 5.3 Trace CSV
Header và thứ tự **chính xác**:
```
Sequence,Operation,ProcessId,RequestedSize,AllocatedSize,Status,Reason,TotalFreeMemory,LargestFreeBlock,InternalFragmentation
```
- Một dòng cho mỗi operation đã xử lý (kể cả thất bại), theo thứ tự `Sequence` tăng dần.
- Lỗi xác thực CSV trước khi chạy không tạo dòng Trace.

| Cột | Kiểu / định dạng | Ý nghĩa |
|---|---|---|
| `Sequence` | Integer | Số thứ tự trong workload |
| `Operation` | `ALLOCATE` / `FREE` | Loại thao tác |
| `ProcessId` | Text | PID đã trim, giữ nguyên chữ hoa/thường |
| `RequestedSize` | Integer hoặc trống | Kích thước yêu cầu gốc; trống cho `FREE` |
| `AllocatedSize` | Integer hoặc trống | Kích thước đã làm tròn khi `ALLOCATE` thành công; hoặc kích thước vùng thu hồi trước khi gộp khi `FREE` thành công |
| `Status` | `SUCCESS` / `FAILED` |
| `Reason` | Mã lỗi ổn định |
| `TotalFreeMemory` | Integer | `FreeMemory` sau thao tác |
| `LargestFreeBlock` | Integer | Kích thước free block lớn nhất sau thao tác (0 nếu không có) |
| `InternalFragmentation` | Integer | Tổng fragmentation nội bộ sau thao tác |

### 5.4 Comparison CSV
**[SPEC]** Header chuẩn:
```
WorkloadName,Algorithm,OperationCount,MemorySize,AllocationUnit,TotalAllocationRequests,SuccessfulAllocations,FailedAllocations,SuccessRate,UsedMemory,FreeMemory,FreeBlockCount,LargestFreeBlock,TotalInternalFragmentation,RuntimeRun1Ms,RuntimeRun2Ms,RuntimeRun3Ms,RuntimeRun4Ms,RuntimeRun5Ms,AverageRuntimeMs
```
- Một dòng cho mỗi trong ba thuật toán.
- Tất cả dòng dùng cùng một cấu hình và workload.
- `SuccessRate` để trống nếu workload không có `ALLOCATE`.
- Cột `Five runtime measurements` chứa năm giá trị thời gian đo (có thể được tách bằng dấu phẩy hoặc định dạng chuỗi tùy thực thi). `AverageRuntimeMs` là trung bình cộng của năm giá trị.

## 6. Xử lý CSV (đọc và xác thực)
- Dùng dấu phẩy làm delimiter; không tự chuyển sang dấu chấm phẩy.
- Header phải khớp **đúng** tên và thứ tự.
- Hỗ trợ quoting chuẩn cho trường chứa dấu phẩy hoặc dấu ngoặc kép.
- Trim khoảng trắng đầu/cuối mỗi ô; bỏ qua dòng hoàn toàn trống.
- Bảo trì lỗi: nếu có bất kỳ lỗi cấu trúc, định dạng, giá trị không dương, duplicate `Sequence`, token `Operation` không hợp lệ, hoặc PID rỗng → từ chối toàn bộ file.
- Thông báo lỗi cần chỉ rõ file/row/field và nguyên nhân.
- Lỗi đầu vào CSV **không** tạo Trace; chỉ ngăn chạy.

## 7. Đặt tên file đầu ra
- **[SPEC]** Tên file phải nhận dạng loại, workload, thuật toán (nếu có) và timestamp.
- **[DESIGN] Mẫu chuẩn:**
  - Simulation Trace: `Trace_{Algorithm}_{WorkloadName}_{yyyyMMdd_HHmmss}.csv`
  - Evaluation Trace: `Trace_{Algorithm}_{WorkloadName}_{yyyyMMdd_HHmmss}.csv` (một file cho mỗi thuật toán, chung timestamp).
  - Evaluation Comparison: `Comparison_{WorkloadName}_{yyyyMMdd_HHmmss}.csv`
- Sử dụng token thuật toán `FirstFit`, `BestFit`, `WorstFit` trong tên.
- Tạo timestamp một lần cho toàn bộ hành động xuất để các file Evaluation có liên kết.
- Nếu tên workload rỗng sau sanitize, dùng `Workload` làm fallback.

## 8. Hợp đồng đo thời gian chạy
- **[SPEC]** Đo mỗi thuật toán 5 lần, báo cả 5 giá trị và trung bình.
- **[DESIGN]** Ghi thời gian bằng millisecond dạng số thập phân; không thêm hậu tố `ms` trong ô CSV.
- Các đo thực hiện với cùng cấu hình/workload bất biến và trạng thái bộ nhớ mới.
- Đo không bao gồm UI, CSV I/O, render chart, hoặc logging.
- Báo cáo phương pháp đo và ngữ cảnh máy trong README.

## 9. Vòng đời kết quả và hủy
- Mỗi workflow có input và kết quả riêng.
- Khi cấu hình hoặc workload thực tế thay đổi sau lượt chạy, vô hiệu hoá kết quả cũ (Trace, Memory Map, chỉ số, bảng, biểu đồ, báo cáo).
- Khi hủy, không hiển thị kết quả giả; UI phải thông báo trạng thái hủy.
- Reset tạo trạng thái mới và xóa trạng thái kết quả của lượt chạy hiện tại.

## 10. Kiểm thử tối thiểu
### 10.1 Kiểm thử domain và thuật toán
- Kiểm tra trạng thái khởi tạo (một region free).
- Kiểm tra chọn region đúng cho ba thuật toán và tie‑breaking.
- Kiểm tra làm tròn, tách, và tiêu thụ region.
- Kiểm tra các mã lỗi nghiệp vụ và không thay đổi trạng thái.
- Kiểm tra gộp vùng free.
- Kiểm tra bất biến vùng, tổng kích thước, và `UsedMemory + FreeMemory == MemorySize`.
- Kiểm tra internal fragmentation tích lũy.

### 10.2 Kiểm thử CSV
- Header đúng, thiếu, thừa, hoặc sai thứ tự.
- Trường quoted, trim, dòng trống, dòng lỗi.
- Số không dương, duplicate Sequence, token Operation không hợp lệ, thiếu `RequestedSize` cho `ALLOCATE`, PID trống.
- Đảm bảo workload không sắp xếp vẫn được xử lý theo `Sequence` tăng dần.
- Đầu ra Trace đúng số dòng, giá trị, trạng thái, lý do.
- Đầu ra Comparison có đúng ba dòng, header, năm thời gian, và `SuccessRate` trống khi không có `ALLOCATE`.
- Kiểm tra tên file và tính nhất quán timestamp.

### 10.3 Kiểm thử tích hợp và hiệu năng
- Simulation: config → workload → một thuật toán → Memory Map/Trace → xuất Trace CSV.
- Evaluation: config → workload → ba thuật toán độc lập → bảng/biểu đồ → ba Trace CSV + Comparison CSV.
- Thay đổi input Simulation chỉ vô hiệu hoá kết quả Simulation; không ảnh hưởng Evaluation, và ngược lại.
- Lỗi xuất một file không xóa các file đã xuất thành công.
- Kiểm thử tải với 100, 1 000, 10 000 thao tác.

## 11. Danh sách kiểm tra triển khai
- [ ] Giữ tài liệu này đồng bộ với `SPECIFICATION.md`.
- [ ] Sử dụng một parser/validator CSV chuẩn cho mỗi loại input.
- [ ] Sử dụng một DTO Trace và một serializer cho cả Simulation và Evaluation.
- [ ] Sử dụng một schema Comparison CSV chuẩn.
- [ ] Đảm bảo thuật toán/domain không phụ thuộc WPF.
- [ ] Đảm bảo vòng đời kết quả Simulation và Evaluation độc lập.
- [ ] Kiểm tra bất biến trạng thái sau thao tác thành công và thất bại.
- [ ] Kiểm tra CSV xuất khớp UI.
- [ ] Cập nhật tài liệu này, tests và README đồng thời khi có thay đổi hợp đồng.
