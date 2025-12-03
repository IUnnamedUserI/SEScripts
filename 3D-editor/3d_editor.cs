/*
ToDo List:
[ ] Реализация редактора 3D-объектов
    [+] Переход курсора с одного экрана на другой
    [+] Событие наведения на ребро
    [ ] Событие нажатия на вершину и ребро
    [ ] Добавление окон для выбора параметров на экран Debug (X, Y, Z, размер, цвет)
    [ ] Добавление кнопок
        [ ] Добавление новой вершины
        [ ] Добавление нового ребра
        [ ] Удаление выбранной вершины
        [ ] Удаление выбранного ребра
    [ ] Создание отдельных объектов
    [ ] Изменение формата хранимых данных

[ ] Построение 3D-модели вертолёта
*/

IMyTextPanel _lcd, _lcdDebug;
IMyCockpit _cockpit;

List<Vertex> _vertexList = new List<Vertex>();
List<Edge> _edgeList = new List<Edge>();
List<Edge> _hoveredEdgeList = new List<Edge>();

MySprite _debugSprite = new MySprite();

float _angle = 0f;
float _verticalAngle = 0f;
float _distance = 30f;
const float _width = 10f;
string _cursorVertex = string.Empty;

Vector2 _cursorPosition = new Vector2(256f, 256f);
Vector2 _cursorDebugPosition = new Vector2(256f, 768f);

Cursor _cursor, _cursorDebug;

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update1;
    Init();
}

void Init()
{
    _vertexList = LoadVertex();
    _edgeList = LoadEdge();
    _cursor = new Cursor(new Vector2(256f, 256f), 0.4f);
    _cursorDebug = new Cursor(_cursorDebugPosition, 0.4f);

    try
    {
        _lcd = (IMyTextPanel)GridTerminalSystem.GetBlockWithName("Text Panel");
        _lcdDebug = (IMyTextPanel)GridTerminalSystem.GetBlockWithName("Debug Text Panel");
        _cockpit = (IMyCockpit)GridTerminalSystem.GetBlockWithName("Control Seat");
    } catch { Echo("Init Error"); }

    SaveVertex();
    if (_edgeList.Count != 0) SaveEdge();
}

void Main(string argument)
{
    if (argument == "cmd:SaveVertex")
    {
        SaveVertex();
        Echo("Вершины сохранены успешно");
    }
    else if (argument == "cmd:LoadVertex")
    {
        _vertexList = LoadVertex();
        _edgeList = LoadEdge();
        Echo("Вершины загружены успешно");
    }
    else if (argument == "cmd:test")
    {
        _vertexList[0] = new Vertex(new Vector3(-1, -5, -1), 10f, Color.Cyan, 0);
    }
    else if (argument == "cmd:SaveEdge") SaveEdge();
    else if (argument == "cmd:LoadEdge") _edgeList = LoadEdge();

    foreach (Vertex _vertex in _vertexList) _vertex.Update2DPosition(_angle, _verticalAngle, _distance);

    _angle += -_cockpit.MoveIndicator.X * 0.01f;
    _verticalAngle += _cockpit.MoveIndicator.Y * 0.01f;
    _distance += _cockpit.MoveIndicator.Z * 0.1f;
    _cursorVertex = string.Empty;

    if (_cockpit.RotationIndicator != new Vector2()) 
    {
        _cursor.MoveCursor(_cockpit.RotationIndicator);
        _cursorDebug.MoveCursor(_cockpit.RotationIndicator);
    }

    // v------------------------ Hover Logic ------------------------v

    foreach (Vertex _vertex in _vertexList)
    {
        Vector2 _currentVertexPosition = new Vector2(260f + _vertex.GetPosition().X * _width, 260f + _vertex.GetPosition().Y * _width);
        Vector2 _tempVectorMin = _currentVertexPosition - _vertex.GetSize() / 2;
        Vector2 _tempVectorMax = _currentVertexPosition + _vertex.GetSize() / 2;

        if (_cursor.GetPosition().X >= _tempVectorMin.X && _cursor.GetPosition().X <= _tempVectorMax.X &&
            _cursor.GetPosition().Y >= _tempVectorMin.Y && _cursor.GetPosition().Y <= _tempVectorMax.Y && _cursorVertex == string.Empty)
        {
            _vertex.ChangeColor(_vertex.GetFocusColor());
            _cursorVertex = $"Вершина (ID: {_vertex.GetID().ToString()})";
        }
        else _vertex.ChangeColor(_vertex.GetDefaultColor());
    }

    // Проверяем рёбра только если не наведены на вершину
    if (_cursorVertex == string.Empty)
    {
        Edge bestEdge = null;
        float bestScore = float.MinValue;
        
        // Находим лучшее ребро для подсветки
        foreach (Edge _edge in _edgeList)
        {
            if (_edge.IsEdgeUnderCursor(_cursor.GetPosition()))
            {
                // Вычисляем "очки" для ребра:
                // 1. Чем ближе к курсору - тем лучше
                // 2. Чем ближе к камере (меньше Z) - тем лучше
                float distanceScore = 1000f / (_edge.GetDistanceToCursor(_cursor.GetPosition()) + 0.1f);
                float zScore = -_edge.GetAverageZ() * 50f; // Меньший Z дает лучший результат
                
                float totalScore = distanceScore + zScore;
                
                if (totalScore > bestScore)
                {
                    bestScore = totalScore;
                    bestEdge = _edge;
                }
            }
        }
        
        // Подсвечиваем только лучшее ребро
        foreach (Edge _edge in _edgeList)
        {
            if (_edge == bestEdge)
            {
                _edge.ChangeColor(_edge.GetFocusColor());
                _cursorVertex = $"Ребро ({_edge.GetParam()})";
            }
            else
            {
                _edge.ChangeColor(_edge.GetDefaultColor());
            }
        }
    }
    else
    {
        // Если наведены на вершину, возвращаем рёбрам стандартный цвет
        foreach (Edge _edge in _edgeList)
        {
            _edge.ChangeColor(_edge.GetDefaultColor());
        }
    }

    // ^------------------------ Hover Logic ------------------------^

    // v------------------------ Main Monitor ------------------------v

    using (MySpriteDrawFrame _frame = _lcd.DrawFrame())
    {
        _frame.Add(_debugSprite);
        foreach (Vertex _vertex in _vertexList)
        {
            _frame.Add(_vertex.DrawVertex(_width));
        }

        foreach (Edge _edge in _edgeList)
        {
            _frame.Add(_edge.Draw(_width));
        }
        
        _frame.Add(_cursor.DrawCursor());
    }

    // ^------------------------ Main Monitor ------------------------^

    // v------------------------ Debug Monitor ------------------------v

    using (MySpriteDrawFrame _frame = _lcdDebug.DrawFrame())
    {
        List<Vector2> _tempVectorList = new List<Vector2>();
        foreach (Vertex _vertex in _vertexList)
        {
            Vector2 _currentVertexPosition = new Vector2(256f + _vertex.GetPosition().X, 256f + _vertex.GetPosition().Y);
            _tempVectorList.Add(_currentVertexPosition);
            Vector2 _tempVectorMin = _currentVertexPosition - _vertex.GetSize();
            Vector2 _tempVectorMax = _currentVertexPosition + _vertex.GetSize();
        }

        _frame.Add(new MySprite(SpriteType.TEXT, $"Координаты курсоры: {Math.Round(_cursor.GetPosition().X)}; {Math.Round(_cursor.GetPosition().Y)}", new Vector2(10f, 450f), null, Color.White, "Debug", TextAlignment.LEFT, 1f));
        _frame.Add(new MySprite(SpriteType.TEXT, "Курсор наведён на: " + _cursorVertex, new Vector2(10f, 480f), null, Color.White, "Debug", TextAlignment.LEFT, 1f));
        _frame.Add(_cursorDebug.DrawCursor());
    }

    // ^------------------------ Debug Monitor ------------------------^
}

void SaveVertex()
{
    string _saveString = string.Empty;
    foreach (Vertex _vertex in _vertexList)
    {
        string _subStr = _vertex.GetParam();
        if (_vertexList.IndexOf(_vertex) < _vertexList.Count - 1) _subStr += "/";
        _saveString += _subStr;
    }
    Me.CustomData = _saveString;
}

List<Vertex> LoadVertex()
{
    _vertexList.Clear();
    List<Vertex> _tempVertexList = new List<Vertex>();
    string[] _data = Me.CustomData.Split('\n')[0].Split('/');
    foreach (string _str in _data)
    {
        string[] _splitedData = _str.Split('|');
        _tempVertexList.Add(new Vertex(
            new Vector3(Convert.ToInt32(_splitedData[0].Split(',')[0]), Convert.ToInt32(_splitedData[0].Split(',')[1]), Convert.ToInt32(_splitedData[0].Split(',')[2])),
            (float)Convert.ToSingle(_splitedData[1]),
            new Color(Convert.ToInt32(Convert.ToInt32(_splitedData[2].Split(',')[0])), Convert.ToInt32(_splitedData[2].Split(',')[1]), Convert.ToInt32(_splitedData[2].Split(',')[2])),
            Convert.ToInt32(_splitedData[3]))
        );
    }
    return _tempVertexList;
}

void SaveEdge()
{
    string _saveString = string.Empty;
    foreach (Edge _edge in _edgeList)
    {
        string _subStr = _edge.GetParam();
        if (_edgeList.IndexOf(_edge) < _edgeList.Count - 1) _subStr += "/";
        _saveString += _subStr;
    }
    if (_saveString != string.Empty) Me.CustomData += $"\n{_saveString}";
    else Me.CustomData += '\n';
}

List<Edge> LoadEdge()
{
    _edgeList.Clear();
    List<Edge> _tempEdgeList = new List<Edge>();
    string[] _data = Me.CustomData.Split('\n')[1].Split('/');
    if (_data[0] != string.Empty)
    {
        foreach (string _str in _data)
        {
            Vertex _tempVertex1 = null, _tempVertex2 = null;
            string[] _splitedData = _str.Split('|');

            foreach (Vertex _vertex in _vertexList)
            {
                if (Convert.ToInt32(_splitedData[0]) == _vertex.GetID()) _tempVertex1 = _vertex;
                if (Convert.ToInt32(_splitedData[1]) == _vertex.GetID()) _tempVertex2 = _vertex;
            }
            if (_tempVertex1 == null || _tempVertex2 == null) break;

            _tempEdgeList.Add(new Edge(
                _tempVertex1,
                _tempVertex2,
                Convert.ToSingle(_splitedData[2]),
                new Color(Convert.ToInt32(_splitedData[3].Split(',')[0]), Convert.ToInt32(_splitedData[3].Split(',')[1]), Convert.ToInt32(_splitedData[3].Split(',')[2]))
            ));
        }
    }
    return _tempEdgeList;
}

class Cursor
{
    Vector2 _cursorPosition;
    float _cursorSensivity;

    public Cursor(Vector2 CursorPosition, float CursorSensivity)
    {
        _cursorPosition = CursorPosition;
        _cursorSensivity = CursorSensivity;
    }

    public void MoveCursor(Vector2 OffsetCursor)
    {
        _cursorPosition.X += OffsetCursor.Y * _cursorSensivity;
        _cursorPosition.Y += OffsetCursor.X * _cursorSensivity;
    }

    public MySprite DrawCursor()
    {
        return new MySprite(SpriteType.TEXTURE, "Triangle", _cursorPosition, new Vector2(10f, 20f), Color.White, "", TextAlignment.CENTER, (float)(100 * 180 / Math.PI));
    }

    public Vector2 GetPosition()
    {
        return _cursorPosition;
    }
}

class Vertex
{
    Vector3 _vertex3DPosition;
    Vector2 _vertexPosition;
    Vector2 _vertexSize;
    Color _vertexCurrentColor;
    Color _vertexDefaultColor;
    Color _vertexFocusColor;
    int _vertexID;

    public Vertex(Vector3 Vertex3DPosition, float VertexSize, Color VertexColor, int VertexID)
    {
        _vertex3DPosition = Vertex3DPosition;
        _vertexPosition = Get2DPosition(0f, 0f, 0f);
        _vertexSize = new Vector2(VertexSize, VertexSize);
        _vertexDefaultColor = VertexColor;
        _vertexCurrentColor = VertexColor;
        _vertexFocusColor = Color.Red;
        _vertexID = VertexID;
    }

    public void ChangeColor(Color Color)
    {
        _vertexCurrentColor = Color;
    }

    public Vector2 Get2DPosition(float HorizontalAngle, float VerticalAngle, float Distance)
    {   
        double x_rot = _vertex3DPosition.X * Math.Cos(HorizontalAngle) - _vertex3DPosition.Z * Math.Sin(HorizontalAngle);
        double z_rot = _vertex3DPosition.X * Math.Sin(HorizontalAngle) + _vertex3DPosition.Z * Math.Cos(HorizontalAngle);
        Vector3 _rotated = new Vector3(x_rot, _vertex3DPosition.Y, z_rot);

        double y_rot = _rotated.Y * Math.Cos(VerticalAngle) - _rotated.Z * Math.Sin(VerticalAngle);
        double z_vrot = _rotated.Y * Math.Sin(VerticalAngle) + _rotated.Z * Math.Cos(VerticalAngle);
        _rotated = new Vector3(_rotated.X, y_rot, z_vrot);

        _rotated.Z += 10;

        Vector2 _projected = new Vector2(
            _rotated.X * Distance / _rotated.Z,
            _rotated.Y * Distance / _rotated.Z
        );
        return _projected;
    }

    public void Update2DPosition(float HorizontalAngle, float VerticalAngle, float Distance)
    {
        _vertexPosition = Get2DPosition(HorizontalAngle, VerticalAngle, Distance);
    }

    public MySprite DrawVertex()
    {
        return new MySprite(SpriteType.TEXTURE, "Circle", _vertexPosition, _vertexSize, _vertexCurrentColor, "", TextAlignment.CENTER, 0f);
    }

    public MySprite DrawVertex(float Width)
    {
        Vector2 _vertexOffsetPosition = new Vector2(256f + _vertexPosition.X * Width, 256f + _vertexPosition.Y * Width);
        return new MySprite(SpriteType.TEXTURE, "Circle", _vertexOffsetPosition, _vertexSize, _vertexCurrentColor, "", TextAlignment.CENTER, 0f);
    }

    public Vector2 GetPosition()
    {
        return _vertexPosition;
    }

    public Vector3 GetPosition3D()
    {
        return _vertex3DPosition;
    }

    public Vector2 GetSize()
    {
        return _vertexSize;
    }

    public int GetID()
    {
        return _vertexID;
    }

    public Color GetFocusColor()
    {
        return _vertexFocusColor;
    }

    public Color GetDefaultColor()
    {
        return _vertexDefaultColor;
    }

    public string GetParam()
    {
        string _temp3DPosition = $"{_vertex3DPosition.X},{_vertex3DPosition.Y},{_vertex3DPosition.Z}";
        string _tempSize = $"{Math.Abs(_vertexSize.X)}";
        string _tempColor = $"{_vertexDefaultColor.R},{_vertexDefaultColor.G},{_vertexDefaultColor.B}";
        return $"{_temp3DPosition}|{_tempSize}|{_tempColor}|{_vertexID}";
    }
}

class Edge
{
    public Vertex _vertex1;
    public Vertex _vertex2;
    float _edgeWidth;
    Color _edgeCurrentColor;
    Color _edgeDefaultColor;
    Color _edgeFocusColor;

    public Edge(Vertex Vertex1, Vertex Vertex2, float Width, Color Color)
    {
        _vertex1 = Vertex1;
        _vertex2 = Vertex2;
        _edgeWidth = Width;
        _edgeDefaultColor = Color;
        _edgeCurrentColor = Color;
        _edgeFocusColor = Color.Red;
    }

    public MySprite Draw(float Width)
    {
        Vector2 _vertex1Position = new Vector2(256f + _vertex1.GetPosition().X * Width, 256f + _vertex1.GetPosition().Y * Width);
        Vector2 _vertex2Position = new Vector2(256f + _vertex2.GetPosition().X * Width, 256f + _vertex2.GetPosition().Y * Width);
        Vector2 CenterVector = (_vertex1Position + _vertex2Position) / 2;
        return new MySprite(
            SpriteType.TEXTURE,
            "SquareSimple",
            CenterVector,
            new Vector2((_vertex1Position - _vertex2Position).Length(), _edgeWidth),
            _edgeCurrentColor,
            "",
            TextAlignment.CENTER,
            (float)Math.Atan2(_vertex2Position.Y - _vertex1Position.Y, _vertex2Position.X - _vertex1Position.X));
    }

    public Color GetDefaultColor()
    {
        return _edgeDefaultColor;
    }

    public Color GetFocusColor()
    {
        return _edgeFocusColor;
    }

    public void ChangeColor(Color _color)
    {
        _edgeCurrentColor = _color;
    }

    public string GetParam()
    {
        string _vertex1ID = _vertex1.GetID().ToString();
        string _vertex2ID = _vertex2.GetID().ToString();
        string _edgeColorString = $"{_edgeDefaultColor.R},{_edgeDefaultColor.G},{_edgeDefaultColor.B}";
        return $"{_vertex1ID}|{_vertex2ID}|{_edgeWidth.ToString()}|{_edgeColorString}";
    }

    // Получаем экранные координаты вершин с учетом смещения
    private Vector2 GetVertex1ScreenPos(float width)
    {
        return new Vector2(260f + _vertex1.GetPosition().X * width, 260f + _vertex1.GetPosition().Y * width);
    }
    
    private Vector2 GetVertex2ScreenPos(float width)
    {
        return new Vector2(260f + _vertex2.GetPosition().X * width, 260f + _vertex2.GetPosition().Y * width);
    }

    public bool IsEdgeUnderCursor(Vector2 _cursorPosition)
    {
        const float width = 10f; // Используем тот же масштаб, что и при отрисовке
        Vector2 v1 = GetVertex1ScreenPos(width);
        Vector2 v2 = GetVertex2ScreenPos(width);
        
        // Вычисляем вектор ребра
        Vector2 edge = v2 - v1;
        float edgeLength = edge.Length();
        
        // Нормализуем вектор ребра
        Vector2 edgeDir = edge / edgeLength;
        
        // Вектор от первой вершины к курсору
        Vector2 toCursor = _cursorPosition - v1;
        
        // Проекция вектора toCursor на ребро
        float projection = Vector2.Dot(toCursor, edgeDir);
        
        // Ограничиваем проекцию в пределах ребра
        projection = Math.Max(0, Math.Min(edgeLength, projection));
        
        // Находим ближайшую точку на ребре к курсору
        Vector2 closestPoint = v1 + edgeDir * projection;
        
        // Расстояние от курсора до ближайшей точки на ребре
        float distance = Vector2.Distance(_cursorPosition, closestPoint);
        
        // Пороговое расстояние для наведения (можно регулировать)
        float threshold = Math.Max(_edgeWidth, 10f); // Используем ширину ребра или минимальный порог
        
        return distance <= threshold;
    }

    public float GetDistanceToCursor(Vector2 cursorPos)
    {
        const float width = 10f;
        Vector2 v1 = GetVertex1ScreenPos(width);
        Vector2 v2 = GetVertex2ScreenPos(width);
        
        // Вычисляем вектор ребра
        Vector2 edgeVec = v2 - v1;
        float edgeLength = edgeVec.Length();
        
        // Нормализуем вектор ребра
        Vector2 edgeDir = edgeVec / edgeLength;
        
        // Вектор от первой вершины к курсору
        Vector2 toCursor = cursorPos - v1;
        
        // Проекция вектора toCursor на ребро
        float projection = Vector2.Dot(toCursor, edgeDir);
        
        // Ограничиваем проекцию в пределах ребра
        projection = Math.Max(0, Math.Min(edgeLength, projection));
        
        // Находим ближайшую точку на ребре к курсору
        Vector2 closestPoint = v1 + edgeDir * projection;
        
        // Расстояние от курсора до ближайшей точки на ребре
        return Vector2.Distance(cursorPos, closestPoint);
    }

    public float GetAverageZ()
    {
        return (_vertex1.GetPosition3D().Z + _vertex2.GetPosition3D().Z) / 2f;
    }
}