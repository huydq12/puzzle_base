# Conveyor Enter/Exit Endpoint Logic

File chính: `Assets/Scripts/GamePlay/Arrow Flow/Board.cs`

Hai field liên quan:

```csharp
[SerializeField] private GameObject _conveyorEnterInOutPrefab;
[SerializeField] private GameObject _conveyorExitInOutPrefab;
```

Hai prefab này là visual endpoint cho conveyor path dạng mở:

- `_conveyorEnterInOutPrefab`: marker/cổng vào conveyor.
- `_conveyorExitInOutPrefab`: marker/cổng ra conveyor.

Chúng không điều khiển gameplay conveyor chạy. Logic di chuyển conveyor nằm ở `ConveyorController.SetupTracks(...)`. Logic tunnel/chặn line nằm ở `_conveyorTunelPrefab` và các hàm tunnel trong `Board`.

## Nguồn Data

Conveyor path được lấy từ `LevelConfig`:

```csharp
public ConveyorLine ConveyorLine;
public List<ConveyorLine> ConveyorLines = new List<ConveyorLine>();
```

`LevelConfig.GetConveyorLines()` ưu tiên `ConveyorLines`; nếu không có thì fallback sang `ConveyorLine` legacy.

Mỗi `ConveyorLine` có:

```csharp
public List<Vector2Int> Cells = new List<Vector2Int>();
public List<int> Types = new List<int>();
public List<int> Counters = new List<int>();
public List<bool> IsHoles = new List<bool>();
```

- `Cells`: thứ tự cell tạo thành path conveyor.
- `Types`, `Counters`, `IsHoles`: metadata theo index của `Cells`, dùng cho tunnel/hole, không dùng trực tiếp để spawn enter/exit prefab.

## Setup Flow

`Board.SetupLevel(LevelConfig config)` gọi:

```csharp
SetupConveyor();
```

Trong `SetupConveyor()`:

1. Clear state tunnel:
   ```csharp
   _conveyorTunnelBlockCounts.Clear();
   _activeTunnels.Clear();
   ```

2. Lấy conveyor lines:
   ```csharp
   List<ConveyorLine> conveyorLines = _currentConfig.GetConveyorLines();
   ```

3. Reset cell type/renderer từ level config:
   ```csharp
   cell.CellType = _currentConfig.Cells[col, row].CellType;
   cell.ShowRenderer(cell.IsOccupied);
   ```

4. Với từng `ConveyorLine`:
   - bỏ qua nếu null hoặc không có cells.
   - filter cell ngoài bounds.
   - bỏ duplicate.
   - normalize path.
   - yêu cầu path có ít nhất 3 cell hợp lệ.
   - kiểm tra path là closed loop hoặc open chain.

5. Set toàn bộ cell trong path thành conveyor:
   ```csharp
   cell.CellType = GridCellType.Conveyor;
   ```

6. Build spline data cho `ConveyorController`.

7. Nếu path không phải closed loop, spawn enter/exit endpoint:
   ```csharp
   if (!isClosedLoop)
       SpawnOpenConveyorEndpoints(orderedCells);
   ```

8. Spawn tunnel nếu có metadata:
   ```csharp
   SpawnConveyorTunels(orderedCells, metaByCell, cornerOffset);
   ```

9. Gửi spline tracks sang controller:
   ```csharp
   ConveyorController.Instance.SetupTracks(trackSetups);
   ```

## Khi Nào Spawn Enter/Exit

Endpoint prefab chỉ spawn khi conveyor là open chain:

```csharp
if (!isClosedLoop)
    SpawnOpenConveyorEndpoints(orderedCells);
```

Không spawn với closed loop vì loop không có điểm đầu/cuối.

Điều kiện conveyor hợp lệ trước khi spawn:

- `ConveyorLine` không null.
- `Cells` không rỗng.
- sau filter/normalize còn ít nhất 3 cell.
- path phải connected theo thứ tự serialized.
- path là một trong hai loại:
  - closed neighbor loop.
  - open neighbor chain.

Nếu path không hợp lệ, `SetupConveyor()` log warning và bỏ qua conveyor line đó.

## Spawn Vị Trí

Hàm:

```csharp
private void SpawnOpenConveyorEndpoints(List<Vector2Int> orderedCells)
```

Logic:

```csharp
GridCell startCell = GetCellAt(orderedCells[0]);
GridCell startNextCell = GetCellAt(orderedCells[1]);
GridCell endPrevCell = GetCellAt(orderedCells[^2]);
GridCell endCell = GetCellAt(orderedCells[^1]);

SpawnConveyorEndpoint(_conveyorEnterInOutPrefab, startCell, startCell, startNextCell, ConveyorEnterEndpointYawOffset);
SpawnConveyorEndpoint(_conveyorExitInOutPrefab, endCell, endPrevCell, endCell, ConveyorExitEndpointYawOffset);
```

Enter endpoint:

- prefab: `_conveyorEnterInOutPrefab`
- position: `startCell`
- direction: `startCell -> startNextCell`
- yaw offset: `-90f`

Exit endpoint:

- prefab: `_conveyorExitInOutPrefab`
- position: `endCell`
- direction: `endPrevCell -> endCell`
- yaw offset: `90f`

## Rotation Logic

Constants:

```csharp
private const float ConveyorEnterEndpointYawOffset = -90f;
private const float ConveyorExitEndpointYawOffset = 90f;
```

Hàm spawn chung:

```csharp
private void SpawnConveyorEndpoint(
    GameObject prefab,
    GridCell positionCell,
    GridCell fromCell,
    GridCell toCell,
    float yawOffset
)
```

Logic hướng:

```csharp
Vector3 from = fromCell.transform.position;
Vector3 to = toCell.transform.position;
Vector3 dir = to - from;
dir.y = 0f;
```

Nếu direction quá nhỏ:

```csharp
dir = Vector3.forward;
```

Nếu hợp lệ:

```csharp
dir.Normalize();
```

Rotation cuối:

```csharp
Quaternion rotation =
    Quaternion.LookRotation(dir, Vector3.up)
    * Quaternion.Euler(0f, yawOffset, 0f);
```

Nghĩa là:

- prefab nhìn theo hướng conveyor path.
- sau đó bù thêm yaw offset để asset đúng orientation.

## Instantiate Logic

```csharp
GameObject endpoint = Instantiate(prefab);
endpoint.transform.SetParent(transform, false);
endpoint.transform.SetPositionAndRotation(positionCell.transform.position, rotation);
```

Parent của endpoint là `Board.transform`.

Endpoint được đặt đúng world position của cell đầu/cuối.

## Normalize Path

Trước khi spawn endpoint, path được normalize qua:

```csharp
NormalizeConveyorCells(...)
```

Hàm này tự thêm bridge cell trong một số trường hợp:

- hai cell cách nhau 2 ô theo trục X.
- hai cell cách nhau 2 ô theo trục Y.
- hai cell đi chéo 1x1 thì chọn bridge cell tốt hơn theo context trước/sau.

Mục tiêu là đảm bảo conveyor path có thứ tự cell liền nhau để:

- build spline ổn định.
- xác định đầu/cuối path chính xác.
- rotation endpoint theo hướng thực tế của path sau normalize.

## Closed Loop Vs Open Chain

Sau normalize:

```csharp
bool isClosedLoop = IsClosedNeighborLoop(conveyorCells);
bool isOpenChain = IsNeighborChain(conveyorCells);
```

Nếu không phải cả hai:

```csharp
Debug.LogWarning(
    $"ConveyorLine[{conveyorIndex}] cells are not connected in serialized order; skipping conveyor setup instead of auto-connecting them."
);
continue;
```

Ý nghĩa:

- Closed loop: path nối thành vòng, không spawn enter/exit.
- Open chain: path có đầu/cuối, spawn enter/exit.

## Liên Quan Tunnel

Endpoint prefab khác tunnel prefab.

Tunnel logic dùng:

```csharp
[SerializeField] private ConveyorTunel _conveyorTunelPrefab;
```

Tunnel chỉ spawn khi conveyor metadata có `Type`, `Counter`, không phải hole:

```csharp
bool hasMeta =
    metaByCell.TryGetValue(cell, out ConveyorMeta meta)
    && meta.HasAny
    && !meta.IsHole
    && meta.Type != 0;
```

Các cell tunnel liên tiếp có cùng metadata được group lại. Nếu group có ít nhất 2 cell thì instantiate tunnel:

```csharp
ConveyorTunel tunel = Instantiate(_conveyorTunelPrefab);
tunel.Setup(group.meta.Type, group.meta.Counter, worldPositions);
```

Nếu `Counter > 0`, tunnel cells được register để block:

```csharp
RegisterTunnelCells(group.cells);
_activeTunnels.Add((tunel, group.cells));
```

Điểm này không ảnh hưởng trực tiếp tới `_conveyorEnterInOutPrefab` và `_conveyorExitInOutPrefab`.

## Tóm Tắt Trách Nhiệm

`_conveyorEnterInOutPrefab`:

- Chỉ spawn với open conveyor path.
- Đặt tại cell đầu tiên của path.
- Hướng theo đoạn đầu tiên của path.
- Bù yaw `-90f`.

`_conveyorExitInOutPrefab`:

- Chỉ spawn với open conveyor path.
- Đặt tại cell cuối cùng của path.
- Hướng theo đoạn cuối cùng của path.
- Bù yaw `90f`.

Không thuộc trách nhiệm của hai prefab này:

- chuyển động conveyor.
- spline track runtime.
- tunnel blocking.
- counter tunnel.
- hole metadata.
- logic line enter conveyor.

---

# Cube Movement Onto And Along Conveyor

Các file chính:

- `Assets/Scripts/GamePlay/Arrow Flow/Line.cs`
- `Assets/Scripts/GamePlay/Arrow Flow/ConveyorController.cs`
- `Assets/Scripts/GamePlay/Arrow Flow/CubeLine.cs`

Phần endpoint prefab chỉ là visual đầu/cuối path. Logic cube thật sự đi lên và chạy trên conveyor được chia như sau:

- `Line`: quyết định line có đi được tới conveyor không, di chuyển line từng bước trên grid, sau đó request đưa head cube lên conveyor.
- `ConveyorController`: quản lý spline track, path slot, queue insert, cycle dịch cube theo slot.
- `CubeLine`: đại diện từng cube, đổi visual khi lên conveyor, xử lý bị bắn/destroy khỏi conveyor.

## Data Model Của Line

Trong `Line`:

```csharp
[Header("Cubes (0 = tail, last = head)")]
[ReadOnly] public List<CubeLine> Cubes;
```

Quy ước:

- `Cubes[0]`: tail.
- `Cubes[^1]`: head.

Line chỉ đi theo hướng từ `headPrev -> head`.

Hướng grid được tính trong `TryComputeGridDirection()`:

```csharp
CubeLine headPrev = Cubes[^2];
CubeLine head = Cubes[^1];

Vector2Int p0 = headPrev.Cell.Position;
Vector2Int p1 = head.Cell.Position;
_gridDir = board.NormalizeGridDir(p1 - p0);
```

Nếu line ít hơn 2 cube hoặc không có cell hợp lệ thì không move.

## Bắt Đầu Move Line

Entry point:

```csharp
public void MoveLine()
```

Guard:

```csharp
if (_isMoving || _isReverting) return;
if (IsIceLine && RemainingCounter > 0) return;
```

Nghĩa là:

- đang move thì không nhận input mới.
- đang revert thì không nhận input mới.
- ice line còn counter thì chưa được move.

Sau đó:

1. reset state move.
2. lấy `Board`, `head`, `headPos`.
3. tính hướng `_gridDir`.
4. tìm conveyor cell phía trước line.
5. tìm occupied cell phía trước line.
6. build move plan.
7. gọi `StepForward()`.

## Tìm Conveyor Và Obstacle

Trong `MoveLine()`:

```csharp
Vector2Int prev = headPos - _gridDir;

GridCell conveyorCell = board.FindConveyorCell(prev, headPos);
GridCell occupiedCell = board.FindOccupiedCell(prev, headPos);
```

Ý nghĩa:

- `FindConveyorCell(prev, headPos)`: tìm conveyor cell nằm trên đường line đang đẩy tới.
- `FindOccupiedCell(prev, headPos)`: tìm cell bị block bởi cube/object khác.

Nếu conveyor cell bị tunnel block:

```csharp
if (conveyorCell != null && board.IsConveyorCellBlockedByTunnel(conveyorCell.Position))
{
    int distToTunnel = board.GetManhattanDistance(headPos, conveyorCell.Position);
    int distToOccupied = occupiedCell != null
        ? board.GetManhattanDistance(headPos, occupiedCell.Position)
        : int.MaxValue;

    if (distToTunnel <= distToOccupied)
        occupiedCell = conveyorCell;

    conveyorCell = null;
}
```

Nghĩa là tunnel đang block được xem như obstacle nếu nó gần hơn hoặc bằng occupied cell thật.

## Build Move Plan

Hàm:

```csharp
private bool TryBuildMovePlan(Board board, Vector2Int headPos, GridCell conveyorCell, GridCell occupiedCell)
```

Có 3 trường hợp chính.

### 1. Không Có Conveyor Phía Trước

```csharp
if (conveyorCell == null)
{
    _willDefinitelyRevert = true;
    _targetConveyorCell = null;
}
```

- nếu không có occupied cell: revert ngay.
- nếu có occupied cell: line đi tới sát obstacle rồi revert.

Số bước trước obstacle:

```csharp
int distToObstacle = board.GetManhattanDistance(headPos, occupiedCell.Position) - 1;
```

### 2. Có Conveyor Và Không Có Obstacle

```csharp
_targetConveyorCell = conveyorCell;
int distToConveyor = board.GetManhattanDistance(headPos, conveyorCell.Position) - 1;
return ApplyConveyorPlan(distToConveyor);
```

Line sẽ đi tới trước conveyor rồi chờ đưa head cube lên conveyor.

### 3. Có Conveyor Và Có Obstacle

So khoảng cách obstacle với conveyor:

```csharp
int distToObstacle2 = board.GetManhattanDistance(headPos, occupiedCell.Position) - 1;
if (distToObstacle2 < distToConveyor)
{
    _willDefinitelyRevert = true;
    _targetConveyorCell = null;
    return ApplyRevertPlan(distToObstacle2);
}

return ApplyConveyorPlan(distToConveyor);
```

Nếu obstacle gần hơn conveyor, line đi tới obstacle rồi revert. Nếu conveyor gần hơn hoặc bằng, line đi vào conveyor.

## Forward Step Trên Grid

Hàm:

```csharp
private void StepForward()
```

Nếu `_remainingSteps > 0`, line dịch từng cube một cell.

Trước mỗi step:

1. reserve conveyor insert index nếu chuẩn bị vào conveyor.
2. validate target cells.
3. save snapshot để revert được.
4. gọi `DoStepForward(...)`.

## Target Cell Cho Từng Cube

Hàm:

```csharp
private bool TryValidateAndPrepareTargets()
```

Với mỗi cube:

- head đi tới cell kế tiếp theo `_gridDir`.
- body/tail đi vào cell của cube phía trước.

Logic:

```csharp
if (i == Cubes.Count - 1)
{
    Vector2Int next = cube.Cell.Position + _gridDir;
    _targetsBuffer[i] = Board.Instance.GetCellAt(next);
}
else
{
    CubeLine nextCube = Cubes[i + 1];
    _targetsBuffer[i] = nextCube.Cell;
}
```

Vì vậy line di chuyển như rắn:

- head tiến lên.
- cube sau đi vào vị trí cube trước.
- tail nhường cell cũ.

## Tween Move Trên Grid

Hàm:

```csharp
private void DoStepForward(System.Action onComplete)
```

Mỗi cube tween tới target cell:

```csharp
cube.transform.DOMove(targetPos, duration)
    .SetEase(Ease.Linear)
```

Khi tween start:

```csharp
if (!_willDefinitelyRevert && cube == Cubes[0])
{
    if (from != null && from.CubeOnCell == cube)
        from.CubeOnCell = null;
}
```

Nghĩa là khi đang đi về conveyor, chỉ clear occupancy của tail lúc bắt đầu để board ổn định trong lúc tween.

Khi tất cả cube hoàn tất:

```csharp
cube.Cell = to;
```

Nếu không phải plan revert:

```csharp
for (int j = 0; j < Cubes.Count; j++)
{
    CubeLine c = Cubes[j];
    if (c != null && c.Cell != null)
        c.Cell.CubeOnCell = c;
}
Board.Instance.RefreshAllHeadHighlights();
```

Sau đó:

```csharp
FlushPendingDetach();
onComplete?.Invoke();
```

## Revert Logic

Nếu line không thể vào conveyor hoặc đụng obstacle, nó revert bằng history snapshot.

Trước mỗi step forward:

```csharp
SaveSnapshot();
```

Khi revert:

```csharp
StartRevert();
StepBackward();
```

`StepBackward()` pop snapshot:

```csharp
GridCell[] prev = _history.Pop();
```

Mỗi cube tween về cell cũ:

```csharp
cube.transform.DOMove(to.transform.position, duration)
    .SetEase(Ease.Linear)
```

Khi start revert step, chỉ clear occupancy của head:

```csharp
if (cube == Cubes[^1])
{
    if (from != null && from.CubeOnCell == cube)
        from.CubeOnCell = null;
}
```

Khi tất cả cube hoàn tất, commit occupancy lại:

```csharp
c.Cell.CubeOnCell = c;
Board.Instance.RefreshAllHeadHighlights();
StepBackward();
```

Revert chạy tiếp cho tới khi `_history` rỗng.

## Reserve Insert Index Vào Conveyor

Trước khi line tới conveyor, nó reserve index gần conveyor cell:

```csharp
private bool TryReserveConveyorBaseIfNeeded()
```

Logic:

```csharp
int startIndex = ConveyorController.Instance.GetInsertIndexForWorldPosition(_targetConveyorCell.transform.position);
```

Nếu không lấy được index thì chuyển sang revert.

`_reservedConveyorBaseIndex` là encoded track/slot index, do `ConveyorController` tạo.

## Chờ Head Cube Vào Conveyor

Khi `_remainingSteps <= 0` và plan là vào conveyor:

```csharp
if (TryStartWaitingForConveyorEnter())
    return;
```

`TryStartWaitingForConveyorEnter()`:

- kiểm tra target conveyor cell.
- kiểm tra ConveyorController.
- kiểm tra tunnel block lần cuối.
- lấy insert index.
- start coroutine `WaitAndRequestConveyorEnter(...)`.

Coroutine:

```csharp
bool queued = ConveyorController.Instance.TryRequestEnter(
    head,
    insertIndex,
    () => OnHeadInsertedToConveyor(head)
);
```

Nếu queue thành công thì line dừng chờ callback.

Nếu game không còn `Playing`, wait bị ngắt:

```csharp
_conveyorWaitInterruptedByLose = true;
_waitingForConveyorEnter = false;
_isMoving = false;
```

## Khi Head Đã Được Insert Vào Conveyor

Callback:

```csharp
private void OnHeadInsertedToConveyor(CubeLine head)
```

Các bước:

1. kill tween của head.
2. clear `CubeOnCell` khỏi grid cell cũ.
3. set `head.Cell = null` vì cube không còn nằm trên grid.
4. add vào `_pendingDetach`.
5. `FlushPendingDetach()` để remove khỏi `Line.Cubes`.

```csharp
if (head.Cell != null && head.Cell.CubeOnCell == head)
    head.Cell.CubeOnCell = null;

head.Cell = null;

if (!_pendingDetach.Contains(head))
    _pendingDetach.Add(head);

FlushPendingDetach();
```

Nếu line hết cube:

```csharp
_isMoving = false;
Board.Instance.RefreshAllHeadHighlights();
return;
```

Nếu còn cube:

- tiếp tục move line.
- reuse `_gridDir`.
- rebuild move plan từ head mới.
- gọi `StepForward()`.

```csharp
_isMoving = true;
_reuseGridDirNextMove = true;

if (!TryRebuildMovePlanAfterConveyorEnter())
{
    _isMoving = false;
    return;
}

StepForward();
```

Vì vậy line sẽ lần lượt đưa từng head cube lên conveyor cho tới khi hết hoặc bị block.

## Detach Cube Khỏi Line

Hàm:

```csharp
private void FlushPendingDetach()
```

Với mỗi cube pending:

```csharp
Cubes.Remove(_pendingDetach[i]);
_pendingDetach[i].transform.SetParent(Board.Instance.transform, true);
```

Sau đó refresh counter text.

Nếu line hết cube:

```csharp
ReleaseKeyIfNeeded("line_empty");
ConveyorController.Instance.OnLineMoved();
Destroy(gameObject);
```

`OnLineMoved()` tăng `_totalLineMoved` trong `ConveyorController`.

## ConveyorController Track/Slot Model

`ConveyorController` không di chuyển cube trực tiếp theo spline percent từng frame. Nó tạo các slot rời rạc dọc spline.

Class nội bộ:

```csharp
private class PathSlot
{
    public Vector3 Position;
    public Vector3 Forward;
    public Vector3 Left;
    public CubeLine CubeSlot;
}
```

Mỗi slot có:

- `Position`: vị trí trên spline.
- `Forward`: hướng chạy.
- `Left`: vector offset ngang.
- `CubeSlot`: cube đang chiếm slot.

Mỗi track:

```csharp
private class ConveyorTrack
{
    public SplineComputer Spline;
    public MeshRenderer Renderer;
    public List<PathSlot> Paths = new();
    public Queue<EnterRequest> WaitingToEnterQueue = new();
    public Dictionary<int, EnterRequest> InsertsByIndex = new();
}
```

## Setup Track Từ Board

`Board.SetupConveyor()` build `TrackSplineSetup` rồi gọi:

```csharp
ConveyorController.Instance.SetupTracks(trackSetups);
```

Trong `SetupTracks()`:

1. `Clear()`.
2. tạo đủ `SplineComputer` cho số track.
3. set spline points.
4. close hoặc break spline theo `IsClosedLoop`.
5. rebuild spline.
6. gọi `BuildPathSlots(track)`.
7. spawn arrow dọc spline.
8. start cycle loop.

## Build PathSlot

Hàm:

```csharp
private void BuildPathSlots(ConveyorTrack track)
```

Tính chiều dài spline:

```csharp
float length = track.Spline.CalculateLength();
```

Số slot:

```csharp
float distancePerCube = Mathf.Max(0.01f, _cubeSize);
int slotCount = Mathf.RoundToInt(length / distancePerCube);
slotCount = Mathf.Max(2, slotCount);
```

Đi từng khoảng đều nhau trên spline:

```csharp
float stepDistance = length / slotCount;
percent = track.Spline.Travel(percent, stepDistance, out moved);
```

Mỗi sample tạo `PathSlot`:

```csharp
Position = sample.position;
Forward = normalized sample.forward trên mặt phẳng XZ;
Left = -Vector3.Cross(Vector3.up, forward);
CubeSlot = null;
```

## Insert Index

Line hỏi conveyor index gần world position:

```csharp
public int GetInsertIndexForWorldPosition(Vector3 worldPos)
```

Hàm tìm slot gần nhất trong tất cả track.

Sau đó dùng dot với hướng conveyor để quyết định insert trước hay sau slot gần nhất:

```csharp
Vector3 conveyorDir = _tracks[bestTrackIndex].Paths[bestSlotIndex].Forward;
Vector3 toWorldPos = (worldPos - slot.Position).normalized;
float dot = Vector3.Dot(toWorldPos, conveyorDir);

int idx = dot > 0f ? (bestSlotIndex + 1) % pathCount : bestSlotIndex;
```

Index được encode thành một int:

```csharp
return EncodeTrackSlot(bestTrackIndex, idx);
```

Encode:

```csharp
return (trackIndex << TrackIndexShift) | (slotIndex & TrackSlotMask);
```

## Queue Request Enter

Line gọi:

```csharp
ConveyorController.Instance.TryRequestEnter(head, insertIndex, onInserted)
```

`TryRequestEnter()`:

- decode track/slot.
- nếu track full thì lose.
- enqueue `EnterRequest`.

```csharp
track.WaitingToEnterQueue.Enqueue(new EnterRequest
{
    Cube = cube,
    Line = cube.Line,
    PreferredIndex = localIndex,
    OnInserted = onInserted
});
```

Request không insert ngay lập tức. Nó được xử lý trong conveyor cycle.

## Conveyor Cycle Loop

Conveyor chạy bằng coroutine:

```csharp
private IEnumerator CycleLoop()
```

Cycle time:

```csharp
private float GetCycleTime()
{
    int speed = Mathf.Max(1, _walkAroundSpeed);
    return 1f / speed;
}
```

Mỗi cycle:

1. nếu paused thì wait.
2. với từng track:
   - nếu có queue nhưng track full thì lose.
   - collect insert requests.
   - chạy slot từ cuối về đầu.
   - insert cube vào preferred slot nếu trống.
   - dịch cube từ previous slot sang current slot.
   - tween cube tới vị trí slot mới.

## Collect Insert Requests

Hàm:

```csharp
private bool TryCollectInsertRequests(ConveyorTrack track)
```

Mỗi request lấy `PreferredIndex`, normalize, rồi đưa vào dictionary:

```csharp
track.InsertsByIndex[idx] = request;
```

Nếu nhiều request cùng index, request sau bị enqueue lại:

```csharp
track.WaitingToEnterQueue.Enqueue(request);
```

## Insert Cube Vào Slot

Trong cycle, nếu slot `i` có request:

```csharp
if (track.InsertsByIndex.TryGetValue(i, out EnterRequest enteringRequest))
{
    if (track.Paths[curIndex].CubeSlot == null)
    {
        track.Paths[curIndex].CubeSlot = enteringRequest.Cube;
        CubeLine cube = track.Paths[curIndex].CubeSlot;
        ...
    }
}
```

Khi cube được insert:

```csharp
cube.SetConveyorVisual();
enteringRequest.Line?.NotifyEnteredConveyor();
enteringRequest.OnInserted?.Invoke();
```

Sau đó tween cube tới slot position:

```csharp
Vector3 targetPos = slot.Position + left * _baseOffsetAmount;
cube.transform.DOMove(targetPos, timePerCycle);
cube.transform.LookAt(targetPos + dir);
```

Cuối cùng:

```csharp
OnAddToPath();
```

`OnAddToPath()` tăng `_totalPathSlotTaken` và update UI percent.

## Dịch Cube Trên Conveyor

Trong cycle, nếu slot hiện tại trống:

```csharp
track.Paths[curIndex].CubeSlot = track.Paths[prevIndex].CubeSlot;
track.Paths[prevIndex].CubeSlot = null;
```

Nghĩa là mỗi cycle cube dịch sang slot kế tiếp.

Nếu current slot đã có cube:

```csharp
standStill = true;
```

Cube không dịch vào slot đang bị chiếm.

Sau khi slot nhận cube từ previous slot:

```csharp
CubeMoving(track.Paths[curIndex], timePerCycle, hideDuringMove);
```

## CubeMoving

Hàm:

```csharp
private void CubeMoving(PathSlot slot, float time, bool hideUntilArrived = false)
```

Target position:

```csharp
Vector3 targetPos = slot.Position + left * _baseOffsetAmount;
```

`_baseOffsetAmount` dùng để offset cube sang bên trái path so với center spline.

Tween:

```csharp
cube.transform.DOMove(targetPos, time)
    .SetEase(Ease.Linear)
    .OnComplete(() =>
    {
        cube.SetRuntimeHidden(false);
    });
```

Rotation:

```csharp
cube.transform.LookAt(targetPos + dir);
```

Nếu cần hide trong lúc wrap từ cuối về đầu:

```csharp
cube.SetRuntimeHidden(true);
```

Khi tween complete thì restore renderer/collider bằng `SetRuntimeHidden(false)`.

## Open Path Wrap/Hide

Trong cycle có logic `tempCubeSlot` và `hideDuringMove` để xử lý cube ở cuối path.

Khi cube từ cuối quay về đầu hoặc cần nhảy slot trong trường hợp insert, cube có thể được hide trong lúc tween:

```csharp
hideDuringMove = tempCubeSlot != null;
```

Sau đó `CubeMoving(..., hideDuringMove)` sẽ gọi:

```csharp
cube.SetRuntimeHidden(true);
```

Điều này tránh thấy cube teleport/ngược path khi slot wrap.

## CubeLine Visual Khi Lên Conveyor

Trong `CubeLine`:

```csharp
public void SetConveyorVisual()
```

Khi cube lên conveyor:

- stop warning head effect.
- disable outline.
- disable head renderer.
- chuyển visual về `CubeType.Normal`.
- chỉ bật renderer normal.
- tắt `_doubleHeadCube`.

Logic:

```csharp
if (_warningHeadEffect != null)
    _warningHeadEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

if (_outline != null)
    _outline.enabled = false;

if (_head != null)
    _head.enabled = false;

RefreshColorAndMaterials(CubeType.Normal);

foreach (var pair in _renderers)
    pair.Value.enabled = pair.Key == CubeType.Normal;

if (_doubleHeadCube != null)
    _doubleHeadCube.gameObject.SetActive(false);
```

Vì vậy cube đã lên conveyor không còn được render như head/corner/head-highlight nữa.

## Hide Runtime Của CubeLine

Trong `CubeLine`:

```csharp
public void SetRuntimeHidden(bool hidden)
```

Khi `hidden = true`:

- cache trạng thái enabled của tất cả `Renderer`.
- tắt renderer.
- cache trạng thái enabled của tất cả `Collider`.
- tắt collider.

Khi `hidden = false`:

- restore renderer enabled theo cache.
- restore collider enabled theo cache.
- clear cache.

Dùng chủ yếu khi conveyor cần hide cube trong lúc wrap/move đặc biệt.

## Cube Bị Bắn Khi Đang Trên Conveyor

Trong `CubeLine.OnHit()`:

```csharp
ConveyorController.Instance.RemoveCubeFromPath(this);
SpawnHitEffect();
transform.DOScale(0f, 0.1f).OnComplete(() => Destroy(gameObject));
```

`RemoveCubeFromPath()` trong `ConveyorController`:

```csharp
paths[i].CubeSlot = null;
_totalPathSlotTaken--;
cube.transform.DOKill();
UpdatePercent();
```

Nghĩa là cube bị remove khỏi slot conveyor trước, rồi animation scale về 0 và destroy.

## Percent Và Lose Warning

`ConveyorController` giữ:

```csharp
private int _totalPathSlotTaken;
```

Mỗi cube vào path:

```csharp
_totalPathSlotTaken++;
UpdatePercent();
```

Mỗi cube bị remove:

```csharp
_totalPathSlotTaken--;
UpdatePercent();
```

Percent:

```csharp
percent = (_totalPathSlotTaken / (float)totalSlots) * 100f;
```

Update UI:

```csharp
UIBottomInGame.SetConveyorPercent(percent);
UITopInGame.SetConveyorWarning(percent >= 70f);
```

Nếu conveyor đầy và vẫn còn request enter:

```csharp
LoseGame();
```

`LoseGame()`:

- stop conveyor.
- set game result/lose.
- play failed SFX/haptic.
- show `UIPauseLose`.

## Pause/Resume Conveyor

`PauseConveyor()`:

```csharp
_isPaused = true;
cube.transform.DOPause();
```

`ResumeConveyor()`:

```csharp
_isPaused = false;
cube.transform.DOPlay();
```

`StopConveyor()`:

- `_isRunning = false`
- `_isPaused = true`
- stop cycle coroutine.
- kill active cube tweens.
- force restore hidden cubes.

## End-To-End Summary

1. Player taps/moves a `Line`.
2. `Line.MoveLine()` checks whether it can move and whether conveyor exists ahead.
3. If no conveyor or obstacle before conveyor, line moves then reverts.
4. If conveyor is reachable, line moves step by step toward it.
5. At conveyor edge, line requests insert for its head cube.
6. `ConveyorController` queues request.
7. In next conveyor cycle, if preferred slot is free, cube is inserted.
8. `CubeLine.SetConveyorVisual()` converts cube to conveyor visual.
9. `Line.OnHeadInsertedToConveyor()` detaches that cube from the line.
10. If line still has cubes, it rebuilds plan and continues feeding the next head cube.
11. `ConveyorController.CycleLoop()` shifts cubes through discrete `PathSlot`s.
12. Each shift uses `CubeMoving()` with linear `DOMove`.
13. If conveyor fills up while insert requests are waiting, lose triggers.
14. If cube is shot/destroyed, `CubeLine.OnHit()` removes it from conveyor path and destroys it.
