# Kế hoạch phân chia module và task nhóm

> **Dự án:** OS Memory Allocation Simulator — Đề tài 11  
> **Tài liệu căn cứ:** `SPECIFICATION.md`, `docs/architecture/module-contracts-and-csv-schema.md` và các quyết định nhóm đã thống nhất.

## 1. Mục đích và nguyên tắc

Tài liệu này dùng để chia phạm vi công việc, giao task, theo dõi tiến độ và kiểm tra kết quả bàn giao. Tài liệu không thay thế đặc tả yêu cầu hoặc hợp đồng module.

- `SPECIFICATION.md` là căn cứ chính về yêu cầu và quy tắc nghiệp vụ.
- `docs/architecture/module-contracts-and-csv-schema.md` xác định hợp đồng dữ liệu, trách nhiệm module và schema CSV.
- Tài liệu này phân rã công việc thành module và task có thể giao cho thành viên.
- Các mã `SIM-01`, `EVAL-01`, `CSV-01` là **mã task**, không phải số thứ tự thành viên và không bắt buộc phản ánh thứ tự thực hiện.
- Tên module mô tả phạm vi chịu trách nhiệm. Một module có thể có nhiều task và có thể do một hoặc nhiều thành viên phối hợp thực hiện.
- Không tự ý thay đổi quy tắc đã chốt. Nếu phát hiện mâu thuẫn hoặc thiếu quyết định, ghi nhận để cả nhóm xử lý trước khi triển khai phần bị ảnh hưởng.

## 2. Bức tranh tổng thể

Ứng dụng có hai luồng người dùng độc lập, cùng sử dụng phần lõi nghiệp vụ và hạ tầng dữ liệu:

```text
Ứng dụng WPF / MVVM
├── SIM — Mô phỏng một thuật toán
│   ├── Cấu hình và chọn dữ liệu
│   ├── Chạy từng bước / chạy liên tục
│   ├── Memory Map, Trace và metrics
│   └── Xuất Trace CSV
│
├── EVAL — Đánh giá ba thuật toán
│   ├── Cấu hình đánh giá riêng
│   ├── Chạy First-Fit, Best-Fit, Worst-Fit độc lập
│   ├── Bảng, biểu đồ và nhận xét
│   └── Xuất ba Trace CSV + một Comparison CSV
│
└── Phần dùng chung
    ├── CORE-MODEL — Model và trạng thái bộ nhớ
    ├── CORE-ALGO — Thuật toán và nghiệp vụ cấp phát/thu hồi
    ├── DATA-CSV — Đọc, kiểm tra và xuất CSV
    └── QA-DOCS — Kiểm thử và tài liệu bàn giao
```

**Ràng buộc kiến trúc:** Evaluation gọi trực tiếp phần lõi dùng chung và khởi tạo trạng thái bộ nhớ mới cho từng thuật toán. Không bắt buộc phải chạy hoặc lấy trạng thái từ màn hình Simulation.

## 3. Danh mục module

| Mã module | Tên module | Phạm vi chịu trách nhiệm | Kết quả chính |
|---|---|---|---|
| `CORE-MODEL` | Model và trạng thái bộ nhớ | Cấu hình, workload operation, memory region/state, trace entry, kết quả và metrics | Các kiểu dữ liệu thống nhất, các bất biến bộ nhớ được kiểm thử |
| `CORE-ALGO` | Thuật toán và nghiệp vụ bộ nhớ | Hợp đồng thuật toán, First-Fit, Best-Fit, Worst-Fit, ALLOCATE/FREE, làm tròn, lỗi nghiệp vụ, gộp vùng trống | Phần lõi chạy được độc lập với WPF và có unit test |
| `DATA-CSV` | Nhập, kiểm tra và xuất CSV | Config CSV, Workload CSV, Trace CSV, Comparison CSV, tên file và lỗi file | CSV đúng schema; lỗi đầu vào được phát hiện trước khi chạy |
| `SIM` | Chức năng Mô phỏng | Cấu hình mô phỏng, điều khiển lượt chạy, Memory Map, Trace, metrics và xuất Trace | Người dùng chạy một thuật toán và theo dõi kết quả |
| `EVAL` | Chức năng Đánh giá | Cấu hình riêng, benchmark ba thuật toán, đo runtime, bảng/biểu đồ và xuất báo cáo | So sánh ba thuật toán trên cùng cấu hình/workload |
| `QA-DOCS` | Kiểm thử và bàn giao | Kiểm thử tích hợp, stress/performance, README, báo cáo, video demo và tổng hợp đóng góp | Có bằng chứng kiểm thử và bộ tài liệu bàn giao |

## 4. Quy ước trạng thái task

Nhóm có thể dùng các trạng thái sau trong bảng quản lý task:

- `Todo`: chưa bắt đầu.
- `In Progress`: đang thực hiện.
- `Review`: đã có bản bàn giao, chờ thành viên khác kiểm tra.
- `Blocked`: bị chặn bởi task hoặc quyết định khác; phải ghi rõ lý do.
- `Done`: đạt tiêu chí hoàn thành và đã được review.

Một task chỉ được đánh dấu `Done` khi code/tài liệu đã được đưa vào repository, tiêu chí nghiệm thu tương ứng đạt và không phá vỡ hợp đồng chung.

## 5. Danh sách task

### 5.1. `CORE-MODEL` — Model và trạng thái bộ nhớ

| Mã task | Công việc | Bàn giao / tiêu chí nghiệm thu | Phụ thuộc |
|---|---|---|---|
| `MODEL-01` | Chốt và tạo model `SimulationConfig` | Có `MemorySize`, `AllocationUnit`; kiểm tra giá trị dương và quy tắc `MemorySize >= AllocationUnit` theo quyết định nhóm | Hợp đồng module |
| `MODEL-02` | Tạo model `WorkloadOperation` | Thể hiện `Sequence`, loại thao tác, `ProcessId`, `RequestedSize`; phân biệt trường cần có cho ALLOCATE và FREE | Hợp đồng module |
| `MODEL-03` | Tạo model vùng nhớ và trạng thái bộ nhớ | Có địa chỉ bắt đầu, kích thước, trạng thái trống/đã cấp phát, PID và kích thước yêu cầu ban đầu khi cần | `MODEL-01` |
| `MODEL-04` | Tạo model `TraceEntry` | Có dữ liệu thao tác, `Status`, `Reason` và metrics sau thao tác theo schema đã thống nhất | `MODEL-02`, hợp đồng CSV |
| `MODEL-05` | Tạo model kết quả mô phỏng và đánh giá | Có Trace, trạng thái cuối và metrics; không buộc ViewModel phải tự tính nghiệp vụ | `MODEL-03`, `MODEL-04` |
| `MODEL-06` | Viết test cho bất biến trạng thái bộ nhớ | Xác minh vùng nhớ bắt đầu từ 0, liên tục, không chồng lấn, tổng kích thước bằng `MemorySize`, không còn hai vùng trống kề nhau sau khi gộp | `MODEL-03` |

### 5.2. `CORE-ALGO` — Thuật toán và nghiệp vụ bộ nhớ

| Mã task | Công việc | Bàn giao / tiêu chí nghiệm thu | Phụ thuộc |
|---|---|---|---|
| `ALGO-01` | Định nghĩa hợp đồng chung cho thuật toán | Ba thuật toán dùng chung kiểu đầu vào/đầu ra; thuật toán không phụ thuộc WPF/ViewModel | `MODEL-03` |
| `ALGO-02` | Cài đặt First-Fit | Chọn vùng trống đầu tiên đủ lớn theo địa chỉ tăng dần; có unit test | `ALGO-01` |
| `ALGO-03` | Cài đặt Best-Fit | Chọn vùng trống đủ lớn nhỏ nhất; hòa thì chọn địa chỉ bắt đầu nhỏ hơn; có unit test | `ALGO-01` |
| `ALGO-04` | Cài đặt Worst-Fit | Chọn vùng trống đủ lớn lớn nhất; hòa thì chọn địa chỉ bắt đầu nhỏ hơn; có unit test | `ALGO-01` |
| `ALGO-05` | Xử lý nghiệp vụ ALLOCATE dùng chung | Chuẩn hóa PID, kiểm tra PID trùng, làm tròn kích thước, phân biệt `REQUEST_EXCEEDS_MEMORY` và `INSUFFICIENT_MEMORY`; thất bại không đổi trạng thái | `ALGO-01`, `MODEL-02`, `MODEL-03` |
| `ALGO-06` | Xử lý nghiệp vụ FREE dùng chung | Kiểm tra PID; PID không tồn tại/đã giải phóng trả `PID_NOT_FOUND`; thu hồi và gộp tất cả vùng trống liền kề | `ALGO-05` |
| `ALGO-07` | Tính metrics sau từng thao tác | Tính metrics sau cả thao tác thành công lẫn thất bại; thất bại giữ nguyên metrics nếu trạng thái không đổi; phân mảnh nội là tổng của các tiến trình đang hoạt động | `ALGO-05`, `ALGO-06`, `MODEL-04` |
| `ALGO-08` | Kiểm thử trường hợp biên và tie-breaking | Có test PID trùng, PID được tái sử dụng sau FREE, không đủ vùng liên tục, yêu cầu lớn hơn bộ nhớ, vùng bằng đúng kích thước và hòa lựa chọn | `ALGO-02`–`ALGO-07` |

**Nguyên tắc phân tách:** First-Fit, Best-Fit và Worst-Fit chỉ quyết định vùng trống được chọn. Logic cấp phát/thu hồi, kiểm tra PID, làm tròn, ghi nhận thất bại và gộp vùng trống nên được dùng chung, tránh viết lại ba lần.

### 5.3. `DATA-CSV` — Nhập, kiểm tra và xuất CSV

| Mã task | Công việc | Bàn giao / tiêu chí nghiệm thu | Phụ thuộc |
|---|---|---|---|
| `CSV-01` | Đọc Config CSV | Đọc đúng header và kiểu dữ liệu theo `module-contracts-and-csv-schema.md`; báo lỗi nếu cấu trúc/giá trị không hợp lệ | `MODEL-01` |
| `CSV-02` | Đọc Workload CSV | Đọc đúng schema; chuẩn hóa PID; xác minh `Sequence` là số nguyên dương, duy nhất; trả workload theo `Sequence` tăng dần | `MODEL-02` |
| `CSV-03` | Kiểm tra toàn bộ dữ liệu trước khi chạy | Phát hiện header/cột/kiểu dữ liệu/trường bắt buộc không hợp lệ; không tạo Trace một phần khi file đầu vào không hợp lệ | `CSV-01`, `CSV-02` |
| `CSV-04` | Tạo thông báo lỗi nhập liệu | Nêu được file/dòng/trường và lý do khi có thể xác định; phân biệt lỗi dữ liệu CSV với lỗi nghiệp vụ trong Trace | `CSV-03` |
| `CSV-05` | Xuất Trace CSV | Xuất đúng header, thứ tự cột, mã trạng thái/lý do và quy tắc ô trống theo hợp đồng | `MODEL-04` |
| `CSV-06` | Xuất Comparison CSV | Một dòng cho mỗi thuật toán; bao gồm các cột: `WorkloadName,Algorithm,OperationCount,MemorySize,AllocationUnit,TotalAllocationRequests,SuccessfulAllocations,FailedAllocations,SuccessRate,UsedMemory,FreeMemory,FreeBlockCount,LargestFreeBlock,TotalInternalFragmentation,RuntimeRun1Ms,RuntimeRun2Ms,RuntimeRun3Ms,RuntimeRun4Ms,RuntimeRun5Ms,AverageRuntimeMs` | `MODEL-05`, kết quả `EVAL` |
| `CSV-07` | Quy tắc tên file và lỗi ghi file | Tên file có loại kết quả, workload, thuật toán khi cần và timestamp; xử lý ký tự không hợp lệ, file trùng tên và lỗi ghi | `CSV-05`, `CSV-06` |
| `CSV-08` | Kiểm thử CSV | Có fixture hợp lệ và không hợp lệ; kiểm tra header, dòng dữ liệu, ô trống, số liệu và khả năng đọc lại kết quả xuất | `CSV-01`–`CSV-07` |

### 5.4. `SIM` — Chức năng Mô phỏng

| Mã task | Công việc | Bàn giao / tiêu chí nghiệm thu | Phụ thuộc |
|---|---|---|---|
| `SIM-01` | Hoàn thiện màn hình cấu hình mô phỏng | Nhập `MemorySize`, `AllocationUnit`; chọn thuật toán và nguồn cấu hình/workload theo giao diện đã thống nhất | `MODEL-01`, thiết kế UI |
| `SIM-02` | Kết nối nạp Config/Workload | Hỗ trợ nhập trực tiếp hoặc chọn CSV theo phạm vi UI; hiển thị lỗi validation rõ ràng | `CSV-01`–`CSV-04` |
| `SIM-03` | Tạo/hoàn thiện `SimulationService` | Nhận cấu hình, workload và thuật toán; khởi tạo bộ nhớ mới; trả kết quả không phụ thuộc UI | `CORE-ALGO`, `MODEL-05` |
| `SIM-04` | Điều khiển Step và Run | Step xử lý một thao tác; Run xử lý liên tục; thứ tự xử lý theo `Sequence` | `SIM-03` |
| `SIM-05` | Pause và Reset | Pause dừng việc tiến hành bước tiếp theo; Reset đưa mô phỏng về trạng thái ban đầu theo hợp đồng | `SIM-04` |
| `SIM-06` | Hiển thị Memory Map | Vẽ/hiển thị các vùng theo `StartAddress` tăng dần, phân biệt vùng trống và đã cấp phát, thể hiện PID/kích thước | `SIM-03` |
| `SIM-07` | Hiển thị Trace và metrics | Trace, Memory Map và metrics phản ánh cùng trạng thái sau mỗi bước; giải thích `Reason` bằng thông báo dễ hiểu | `SIM-04`, `ALGO-07` |
| `SIM-08` | Xuất Trace CSV từ Simulation | Xuất kết quả của lượt chạy hiện tại đúng schema; lỗi xuất không làm đổi trạng thái mô phỏng | `CSV-05`, `CSV-07` |
| `SIM-09` | Quản lý vòng đời kết quả Simulation | Khi cấu hình hoặc workload thực sự thay đổi, xóa/ẩn kết quả cũ; giữ kết quả nếu đầu vào không đổi | `SIM-01`–`SIM-08` |

### 5.5. `EVAL` — Chức năng Đánh giá

| Mã task | Công việc | Bàn giao / tiêu chí nghiệm thu | Phụ thuộc |
|---|---|---|---|
| `EVAL-01` | Hoàn thiện màn hình cấu hình đánh giá riêng | Có cấu hình/workload riêng; không bắt buộc đi qua màn hình Simulation | `MODEL-01`, thiết kế UI |
| `EVAL-02` | Kết nối nạp và validation dữ liệu | Nhận cấu hình/workload hợp lệ từ nhập trực tiếp hoặc CSV; chặn chạy nếu đầu vào không hợp lệ | `CSV-01`–`CSV-04` |
| `EVAL-03` | Tạo/hoàn thiện `EvaluationService` | Nhận trực tiếp cấu hình/workload và gọi phần lõi dùng chung; không điều khiển View Simulation | `CORE-ALGO`, `MODEL-05` |
| `EVAL-04` | Chạy benchmark ba thuật toán độc lập | Mỗi thuật toán bắt đầu từ trạng thái bộ nhớ mới, cùng cấu hình/workload; kết quả Trace và metrics được giữ riêng | `EVAL-03` |
| `EVAL-05` | Tính chỉ số so sánh | Có số yêu cầu cấp phát, số thành công/thất bại, Success Rate và metrics cuối theo hợp đồng | `EVAL-04` |
| `EVAL-06` | Đo thời gian chạy | Đo riêng mỗi thuật toán năm lần và tính trung bình số học; không tính thao tác UI/xuất CSV vào thời gian thuật toán nếu hợp đồng quy định đo phần lõi | `EVAL-04`, hợp đồng module |
| `EVAL-07` | Hiển thị bảng so sánh | Một hàng cho mỗi thuật toán; số liệu khớp kết quả service | `EVAL-05`, `EVAL-06` |
| `EVAL-08` | Hiển thị biểu đồ so sánh | Bốn biểu đồ theo đặc tả/mockup: thành công/thất bại, phân mảnh nội, vùng trống lớn nhất và runtime trung bình | `EVAL-05`, `EVAL-06` |
| `EVAL-09` | Hiển thị nhận xét dựa trên số liệu | Nhận xét phản ánh số liệu thực tế, không khẳng định thuật toán nào luôn tốt nhất | `EVAL-07`, `EVAL-08` |
| `EVAL-10` | Xuất ba Trace và một Comparison CSV | Có tối đa ba Trace CSV (mỗi thuật toán một file) và một Comparison CSV; báo rõ trạng thái từng file nếu một phần xuất thất bại | `CSV-05`–`CSV-07`, `EVAL-04`–`EVAL-06` |
| `EVAL-11` | Quản lý vòng đời kết quả Evaluation | Khi cấu hình/workload thực sự thay đổi, xóa/ẩn kết quả cũ; giữ kết quả nếu đầu vào không đổi | `EVAL-01`–`EVAL-10` |

### 5.6. `QA-DOCS` — Kiểm thử và bàn giao

| Mã task | Công việc | Bàn giao / tiêu chí nghiệm thu | Phụ thuộc |
|---|---|---|---|
| `QA-01` | Thiết lập và duy trì test suite | Có lệnh chạy test rõ ràng; test chạy ổn định trên môi trường phát triển của nhóm | Khởi tạo dự án |
| `QA-02` | Kiểm thử tích hợp Simulation | Kiểm tra luồng cấu hình → chạy → Memory Map/Trace → xuất CSV | `SIM`, `CSV` |
| `QA-03` | Kiểm thử tích hợp Evaluation | Kiểm tra cấu hình → benchmark → bảng/biểu đồ → xuất bốn file | `EVAL`, `CSV` |
| `QA-04` | Stress/performance test | Thử workload cỡ 100, 1.000 và 10.000 thao tác; ghi nhận thời gian, lỗi và điều kiện thử | `CORE-ALGO`, `EVAL` |
| `QA-05` | README và hướng dẫn chạy | Có yêu cầu môi trường, cách build/run/test, định dạng CSV và giới hạn đã biết | Các module đã tích hợp |
| `QA-06` | Báo cáo đồ án | Mô tả bài toán, thiết kế, thuật toán, kiểm thử, kết quả đánh giá và giới hạn | Kết quả kiểm thử/đánh giá |
| `QA-07` | Video demo và tổng hợp đóng góp | Demo được hai luồng độc lập; nêu rõ đóng góp thành viên theo công việc thực tế | Ứng dụng ổn định |
| `QA-08` | Kiểm tra bàn giao cuối | Build sạch, test đạt, tài liệu và file ví dụ cần thiết có trong repository | Tất cả module |

## 6. Quy tắc nghiệm thu chung

Một task được xem là hoàn thành khi:

1. Kết quả bàn giao đúng mô tả và không mâu thuẫn với `SPECIFICATION.md` hoặc hợp đồng module.
2. Code không đặt nghiệp vụ lõi trong XAML/code-behind; ViewModel điều phối qua binding/command và service phù hợp.
3. Có test phù hợp với phần logic được thay đổi; test hiện có không bị phá vỡ.
4. Các trường dữ liệu, mã lỗi và schema CSV dùng đúng hợp đồng chung; không tự tạo biến thể riêng theo từng module.
5. Đã commit/push lên nhánh làm việc theo quy ước Git của nhóm và có thể được thành viên khác review.
6. Nếu task bị chặn bởi quyết định chưa thống nhất, phải ghi rõ vấn đề và không tự đánh dấu hoàn thành bằng giả định.

## 7. Thứ tự triển khai và quan hệ phụ thuộc

Có thể triển khai song song sau khi chốt hợp đồng, nhưng nên đi theo thứ tự tổng quát sau:

1. **Chốt hợp đồng chung:** model, bất biến bộ nhớ, schema CSV, interface thuật toán và metrics.
2. **Làm phần lõi:** `CORE-MODEL` và `CORE-ALGO`; viết unit test sớm.
3. **Làm CSV:** `DATA-CSV` có thể bắt đầu khi schema đã chốt; dùng fixture để kiểm thử độc lập.
4. **Tích hợp Simulation và Evaluation:** hai luồng có thể phát triển song song trên cùng service/model đã ổn định.
5. **Tích hợp UI, xuất file và kiểm thử xuyên suốt.**
6. **Hoàn thiện stress/performance, README, báo cáo và demo.**

Không cần đợi toàn bộ giao diện hoàn thành mới bắt đầu test thuật toán/CSV. Có thể dùng test và dữ liệu mẫu để phát triển độc lập.