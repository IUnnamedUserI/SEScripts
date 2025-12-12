/*
ToDo List:
[ ] Реализация редактора 3D-объектов
    [+] Переход курсора с одного экрана на другой
    [+] Событие наведения на ребро
    [+] Событие нажатия на вершину и ребро
    [ ] Добавление окон для выбора параметров на экран Debug (X, Y, Z, размер, цвет)
        [+] Кординаты
        [ ] Размер
        [ ] Цвет
    [ ] Добавление кнопок
        [ ] Добавление новой вершины
        [ ] Добавление нового ребра
    [+] Удаление выбранной вершины
    [+] Удаление выбранного ребра
    [ ] Изменение формата хранимых данных
    [ ] Создание отдельных объектов

[ ] Построение 3D-модели вертолёта
*/

// Остановка: пофикстить назначение ID вершине при её создании

IMyTextPanel _lcd, _lcdDebug;
IMyCockpit _cockpit;
IMyShipWelder _welder;

List<Vertex> _vertexList = new List<Vertex>();
List<Edge> _edgeList = new List<Edge>();
List<Edge> _hoveredEdgeList = new List<Edge>();
List<Button> buttonList = new List<Button>();

MySprite _debugSprite = new MySprite();

float _angle = 0f;
float _verticalAngle = 0f;
float _distance = 30f;
const float _width = 10f;
string _cursorVertex = string.Empty;
bool _clickFlag = false;
int edgeModifyVertexID = -1;
bool edgeAttachToCursor = false; // Новый флаг для привязки к курсору

Vector3 newVertexPosition = new Vector3(0f, 0f, 0f);

Vector2 _cursorPosition = new Vector2(256f, 256f);
Vector2 _cursorDebugPosition = new Vector2(256f, 768f);

Cursor _cursor, _cursorDebug;
Vertex selectedVertex = null;
Vertex cursorVertex = new Vertex(new Vector3(0, 0, 0), 10, Color.Yellow, -999);
Edge selectedEdge = null;

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
        _welder = (IMyShipWelder)GridTerminalSystem.GetBlockWithName("Welder");
    } catch { Echo("Init Error"); }

    SaveVertex();
    if (_edgeList.Count != 0) SaveEdge();

    buttonList.Add(new Button(buttonList.Count, "Добавить вершину", Color.Green, new Vector2(125f, 25f))); // ID: 0
    buttonList.Add(new Button(buttonList.Count, "Добавить ребро", Color.Green, new Vector2(350f, 25f))); // ID: 1
    buttonList.Add(new Button(buttonList.Count, "<", Color.White, Color.Blue, new Vector2(196f, 150f))); // ID: 2
    buttonList.Add(new Button(buttonList.Count, "<<", Color.White, Color.Blue, new Vector2(136f, 150f))); // ID: 3
    buttonList.Add(new Button(buttonList.Count, ">", Color.White, Color.Blue, new Vector2(316f, 150f))); // ID: 4
    buttonList.Add(new Button(buttonList.Count, ">>", Color.White, Color.Blue, new Vector2(376f, 150f))); // ID: 5
    
    buttonList.Add(new Button(buttonList.Count, "<", Color.White, Color.Blue, new Vector2(196f, 200f))); // ID: 6
    buttonList.Add(new Button(buttonList.Count, "<<", Color.White, Color.Blue, new Vector2(136f, 200f))); // ID: 7
    buttonList.Add(new Button(buttonList.Count, ">", Color.White, Color.Blue, new Vector2(316f, 200f))); // ID: 8
    buttonList.Add(new Button(buttonList.Count, ">>", Color.White, Color.Blue, new Vector2(376f, 200f))); // ID: 9
    
    buttonList.Add(new Button(buttonList.Count, "<", Color.White, Color.Blue, new Vector2(196f, 250f))); // ID: 10
    buttonList.Add(new Button(buttonList.Count, "<<", Color.White, Color.Blue, new Vector2(136f, 250f))); // ID: 11
    buttonList.Add(new Button(buttonList.Count, ">", Color.White, Color.Blue, new Vector2(316f, 250f))); // ID: 12
    buttonList.Add(new Button(buttonList.Count, ">>", Color.White, Color.Blue, new Vector2(376f, 250f))); // ID: 13
    
    buttonList.Add(new Button(buttonList.Count, "", Color.White, Color.Blue, new Vector2(230f, 115f))); // ID: 14
    buttonList.Add(new Button(buttonList.Count, "", Color.White, Color.Blue, new Vector2(230f, 165f))); // ID: 15
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

    if (_welder.IsActivated)
    {
        _clickFlag = true;
    }
    else
    {
        if (_clickFlag)
        {
            OnClick();
            _clickFlag = false;
        }
    }

    if (_cockpit.RollIndicator < 0f)
    {
        if (selectedEdge != null)
        {
            _edgeList.Remove(selectedEdge);
            selectedEdge = null;
            edgeAttachToCursor = false;
        }
        else if (selectedVertex != null)
        {
            for (int i = _edgeList.Count - 1; i >= 0; i--)
            {
                if (_edgeList[i]._vertex1 == selectedVertex || _edgeList[i]._vertex2 == selectedVertex) _edgeList.Remove(_edgeList[i]);
            }
            _vertexList.Remove(selectedVertex);
            selectedVertex = null;
        }
    }

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

    // Обновляем позицию курсорной вершины для режима привязки
    if (edgeAttachToCursor)
    {
        // Преобразуем экранные координаты курсора в 3D позицию для отображения
        // Это упрощенное преобразование - в реальности нужно обратное проецирование
        cursorVertex.UpdatePosition(_cursor.GetPosition());
        
        if (edgeAttachToCursor)
        {
            cursorVertex.UpdatePosition(_cursor.GetPosition());
        }
    }

    // v------------------------ Hover Logic ------------------------v

    foreach (Vertex _vertex in _vertexList)
    {
        if (selectedVertex != _vertex && CheckVertexHover(_vertex))
        {
            _vertex.ChangeColor(_vertex.GetFocusColor());
            _cursorVertex = $"Вершина (ID: {_vertex.GetID().ToString()})";
        }
        else
        {
            if (selectedVertex != _vertex) _vertex.ChangeColor(_vertex.GetDefaultColor());
            else _vertex.ChangeColor(Color.Orange);
        }
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
                float distanceScore = 1000f / (_edge.GetDistanceToCursor(_cursor.GetPosition()) + 0.1f);
                float zScore = -_edge.GetAverageZ() * 50f;
                
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
            if (selectedEdge != _edge)
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
            else
            {
                _edge.ChangeColor(Color.Orange);
            }
        }
    }
    else
    {
        // Если наведены на вершину, возвращаем рёбрам стандартный цвет
        foreach (Edge _edge in _edgeList)
        {
            if (_edge != selectedEdge) _edge.ChangeColor(_edge.GetDefaultColor());
        }
    }

    foreach (Button button in buttonList)
    {
        if (button.IsHovered(_cursor.GetPosition()))
        {
            button.ChangeColor(button.GetFocusColor());
            
            if (selectedEdge != null && (button.GetID() == 14 || button.GetID() == 15))
            {
                foreach (Vertex vertex in _vertexList)
                {
                    if (vertex.GetID() == Convert.ToInt32(button.GetText()))
                    {
                        vertex.ChangeColor(Color.Red);
                    }
                }
            }
        }
        else
        {
            button.ChangeColor(Color.Black);
        }
    }

    // ^------------------------ Hover Logic ------------------------^

    // v------------------------ Main Monitor ------------------------v

    using (MySpriteDrawFrame _frame = _lcd.DrawFrame())
    {
        _frame.Add(_debugSprite);
        
        // Отрисовываем все вершины
        foreach (Vertex _vertex in _vertexList)
        {
            _frame.Add(_vertex.DrawVertex(_width));
        }

        // Отрисовываем временную вершину курсора в режиме привязки
        if (edgeAttachToCursor)
        {
            // Используем специальный метод для отрисовки курсорной вершины
            _frame.Add(cursorVertex.DrawCursorVertex());
        }

        // Отрисовываем рёбра
        foreach (Edge _edge in _edgeList)
        {
            if (edgeAttachToCursor && _edge == selectedEdge)
            {
                // В режиме привязки рисуем ребро от курсора к другой вершине
                if (edgeModifyVertexID == 1)
                {
                    // Меняем первую вершину - рисуем от курсора ко второй вершине
                    _frame.Add(_edge.DrawToCursor(_width, cursorVertex.GetPosition(), 1));
                }
                else if (edgeModifyVertexID == 2)
                {
                    // Меняем вторую вершину - рисуем от первой вершины к курсору
                    _frame.Add(_edge.DrawToCursor(_width, cursorVertex.GetPosition(), 2));
                }
            }
            else
            {
                _frame.Add(_edge.Draw(_width));
            }
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
        }
        
        foreach (Button button in buttonList)
        {
            if (selectedEdge != null)
            {
                if (button.GetID() < 2 || button.GetID() > 13) _frame.AddRange(button.Draw());
            }
            else if (selectedEdge == null)
            {
                if (button.GetID() != 14 && button.GetID() != 15) _frame.AddRange(button.Draw());
            }
        }

        if (selectedVertex != null)
        {
            _frame.Add(new MySprite(SpriteType.TEXT, $"Выделенная вершина: {selectedVertex.GetID().ToString()}", new Vector2(10f, 420f), null, Color.White, "Debug", TextAlignment.LEFT, 1f));
        }
        else if (selectedEdge != null)
        {
            _frame.Add(new MySprite(SpriteType.TEXT, $"Выделенное ребро: {selectedEdge._vertex1.GetID()}-{selectedEdge._vertex2.GetID()}", new Vector2(10f, 420f), null, Color.White, "Debug", TextAlignment.LEFT, 1f));
        }

        if (selectedEdge == null)
        {
            _frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(256f, 150f), new Vector2(50f, 50f), Color.White, "", TextAlignment.CENTER, 0f));
            _frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(256f, 150f), new Vector2(48f, 48f), Color.Black, "", TextAlignment.CENTER, 0f));

            _frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(256f, 200f), new Vector2(50f, 50f), Color.White, "", TextAlignment.CENTER, 0f));
            _frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(256f, 200f), new Vector2(48f, 48f), Color.Black, "", TextAlignment.CENTER, 0f));

            _frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(256f, 250f), new Vector2(50f, 50f), Color.White, "", TextAlignment.CENTER, 0f));
            _frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(256f, 250f), new Vector2(48f, 48f), Color.Black, "", TextAlignment.CENTER, 0f));
        }

        if (selectedVertex == null && selectedEdge == null)
        {
            _frame.Add(new MySprite(SpriteType.TEXT, newVertexPosition.X.ToString(), new Vector2(256f, 135f), null, Color.White, "Debug", TextAlignment.CENTER, 1f));
            _frame.Add(new MySprite(SpriteType.TEXT, newVertexPosition.Y.ToString(), new Vector2(256f, 185f), null, Color.White, "Debug", TextAlignment.CENTER, 1f));
            _frame.Add(new MySprite(SpriteType.TEXT, newVertexPosition.Z.ToString(), new Vector2(256f, 235f), null, Color.White, "Debug", TextAlignment.CENTER, 1f));
        }
        else if (selectedVertex != null && selectedEdge == null)
        {
            Vector3 tempVector = selectedVertex.GetPosition3D();
            _frame.Add(new MySprite(SpriteType.TEXT, tempVector.X.ToString(), new Vector2(256f, 135f), null, Color.White, "Debug", TextAlignment.CENTER, 1f));
            _frame.Add(new MySprite(SpriteType.TEXT, tempVector.Y.ToString(), new Vector2(256f, 185f), null, Color.White, "Debug", TextAlignment.CENTER, 1f));
            _frame.Add(new MySprite(SpriteType.TEXT, tempVector.Z.ToString(), new Vector2(256f, 235f), null, Color.White, "Debug", TextAlignment.CENTER, 1f));
        }
        else if (selectedVertex == null && selectedEdge != null)
        {
            buttonList[14].SetText(selectedEdge._vertex1.GetID().ToString());
            buttonList[15].SetText(selectedEdge._vertex2.GetID().ToString());
            _frame.Add(new MySprite(SpriteType.TEXT, $"ID вершины A: ", new Vector2(20f, 100f), null, Color.White, "Debug", TextAlignment.LEFT, 1f));
            _frame.Add(new MySprite(SpriteType.TEXT, $"ID вершины B: ", new Vector2(20f, 140f), null, Color.White, "Debug", TextAlignment.LEFT, 1f));
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

void OnClick()
{
    if (edgeAttachToCursor)
    {
        // В режиме привязки: ищем вершину под курсором для прикрепления
        Vertex targetVertex = null;
        
        foreach (Vertex vertex in _vertexList)
        {
            if (CheckVertexHover(vertex))
            {
                targetVertex = vertex;
                break;
            }
        }
        
        if (targetVertex != null && selectedEdge != null)
        {
            // Прикрепляем ребро к найденной вершине
            if (edgeModifyVertexID == 1)
            {
                selectedEdge._vertex1 = targetVertex;
            }
            else if (edgeModifyVertexID == 2)
            {
                selectedEdge._vertex2 = targetVertex;
            }
            
            Echo($"Ребро прикреплено к вершине ID: {targetVertex.GetID()}");
        }
        else
        {
            Echo("Привязка отменена (не найдена вершина под курсором)");
        }
        
        // Выходим из режима привязки
        edgeAttachToCursor = false;
        edgeModifyVertexID = -1;
        return;
    }

    bool isClickVertex = false;
    bool isClickEdge = false;

    foreach (Vertex _vertex in _vertexList)
    {
        if(CheckVertexHover(_vertex))
        {
            isClickVertex = true;
            selectedVertex = _vertex;
            selectedEdge = null;
        }
    }

    if (!isClickVertex)
    {
        foreach (Edge edge in _edgeList)
        {
            if (edge.IsEdgeUnderCursor(_cursor.GetPosition()))
            {
                isClickEdge = true;
                selectedVertex = null;
                selectedEdge = edge;
            }
        }
    }

    if (isClickVertex)
    {
        selectedVertex.ChangeColor(Color.Orange);
        return;
    }
    else if (isClickEdge)
    {
        selectedEdge.ChangeColor(Color.Orange);
        return;
    }

    foreach (Button btn in buttonList)
    {
        if (btn.IsHovered(_cursor.GetPosition()))
        {
            switch (btn.GetID())
            {
                case 0:
                    _vertexList.Add(new Vertex(newVertexPosition, 10f, Color.Cyan, CheckVertexID()));
                    newVertexPosition = new Vector3(0f, 0f, 0f);
                    break;

                case 1: break;

                case 2:
                    if (selectedEdge == null)
                    {
                        if (selectedVertex == null) newVertexPosition.X--;
                        else
                        {
                            selectedVertex.X--;
                            selectedVertex.Update();
                        }
                    }
                    break;

                case 3:
                    if (selectedEdge == null)
                    {
                        if (selectedVertex == null) newVertexPosition.X -= 5;
                        else
                        {
                            selectedVertex.X -= 5;
                            selectedVertex.Update();
                        }
                    }
                    break;

                case 4:
                    if (selectedEdge == null)
                    {
                        if (selectedVertex == null) newVertexPosition.X++;
                        else
                        {
                            selectedVertex.X++;
                            selectedVertex.Update();
                        }
                    }
                    break;

                case 5:
                    if (selectedEdge == null)
                    {
                        if (selectedVertex == null) newVertexPosition.X += 5;
                        else
                        {
                            selectedVertex.X += 5;
                            selectedVertex.Update();
                        }
                    }
                    break;
                
                case 6:
                    if (selectedEdge == null)
                    {
                        if (selectedVertex == null) newVertexPosition.Y--;
                        else
                        {
                            selectedVertex.Y--;
                            selectedVertex.Update();
                        }
                    }
                    break;

                case 7:
                    if (selectedEdge == null)
                    {
                        if (selectedVertex == null) newVertexPosition.Y -= 5;
                        else
                        {
                            selectedVertex.Y -= 5;
                            selectedVertex.Update();
                        }
                    }
                    break;

                case 8:
                    if (selectedEdge == null)
                    {
                        if (selectedVertex == null) newVertexPosition.Y++;
                        else
                        {
                            selectedVertex.Y++;
                            selectedVertex.Update();
                        }
                    }
                    break;

                case 9:
                    if (selectedEdge == null)
                    {
                        if (selectedVertex == null) newVertexPosition.Y += 5;
                        else
                        {
                            selectedVertex.Y += 5;
                            selectedVertex.Update();
                        }
                    }
                    break;

                case 10:
                    if (selectedEdge == null)
                    {
                        if (selectedVertex == null) newVertexPosition.Z--;
                        else
                        {
                            selectedVertex.Z--;
                            selectedVertex.Update();
                        }
                    }
                    break;

                case 11:
                    if (selectedEdge == null)
                    {
                    if (selectedVertex == null) newVertexPosition.Z -= 5;
                        else
                        {
                            selectedVertex.Z -= 5;
                            selectedVertex.Update();
                        } 
                    }
                    break;

                case 12:
                    if (selectedEdge == null)
                    {
                    if (selectedVertex == null) newVertexPosition.Z++;
                        else
                        {
                            selectedVertex.Z++;
                            selectedVertex.Update();
                        } 
                    }
                    break;

                case 13:
                    if (selectedEdge == null)
                    {
                        if (selectedVertex == null) newVertexPosition.Z += 5;
                        else
                        {
                            selectedVertex.Z += 5;
                            selectedVertex.Update();
                        }
                    }
                    break;
                
                case 14:
                    if (selectedEdge != null)
                    {
                        // Включаем режим привязки для первой вершины
                        edgeAttachToCursor = true;
                        edgeModifyVertexID = 1;
                        Echo("Режим привязки: двигайте курсор к новой вершине и нажмите");
                    }
                    break;
                    
                case 15:
                    if (selectedEdge != null)
                    {
                        // Включаем режим привязки для второй вершины
                        edgeAttachToCursor = true;
                        edgeModifyVertexID = 2;
                        Echo("Режим привязки: двигайте курсор к новой вершине и нажмите");
                    }
                    break;
            }
            return;
        }
    }

    selectedEdge = null;
    selectedVertex = null;
}

bool CheckVertexHover(Vertex vertex)
{
    Vector2 _currentVertexPosition = new Vector2(260f + vertex.GetPosition().X * _width, 260f + vertex.GetPosition().Y * _width);
    Vector2 _tempVectorMin = _currentVertexPosition - vertex.GetSize() / 2;
    Vector2 _tempVectorMax = _currentVertexPosition + vertex.GetSize() / 2;

    if (_cursor.GetPosition().X >= _tempVectorMin.X && _cursor.GetPosition().X <= _tempVectorMax.X &&
        _cursor.GetPosition().Y >= _tempVectorMin.Y && _cursor.GetPosition().Y <= _tempVectorMax.Y)
    {
        return true;
    }
    return false;
}

//  0 1 2 3 4 5 6 7 9
// -1 0 1 2 3 4 5 6 7 8 9 10

int CheckVertexID()
{
    int id = -1;
    foreach (Vertex vertex in _vertexList)
    {
        if (vertex.GetID() == id + 1) id++;
        else return id;
    }
    return ++id;
}

// v------------------------------------------------------------------------- Классы ---------------------------------------------------------------------------v

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
    public float X;
    public float Y;
    public float Z;

    public Vertex(Vector3 Vertex3DPosition, float VertexSize, Color VertexColor, int VertexID)
    {
        _vertex3DPosition = Vertex3DPosition;
        _vertexPosition = Get2DPosition(0f, 0f, 0f);
        _vertexSize = new Vector2(VertexSize, VertexSize);
        _vertexDefaultColor = VertexColor;
        _vertexCurrentColor = VertexColor;
        _vertexFocusColor = Color.Red;
        _vertexID = VertexID;
        this.X = Vertex3DPosition.X;
        this.Y = Vertex3DPosition.Y;
        this.Z = Vertex3DPosition.Z;
    }

    public Vertex()
    {
        _vertex3DPosition = new Vector3(0f, 0f, 0f);
        _vertexPosition = new Vector2(0f, 0f);
        _vertexSize = new Vector2(0f, 0f);
        _vertexDefaultColor = Color.Yellow;
        _vertexCurrentColor = Color.Yellow;
        _vertexFocusColor = Color.Yellow;
        _vertexID = -999;
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

    public void Update()
    {
        _vertex3DPosition = new Vector3(X, Y, Z);
    }

    public void UpdatePosition(Vector2 Position)
    {
        _vertexPosition = Position;
        
        float screenX = (Position.X - 256f) / _width;
        float screenY = (Position.Y - 256f) / _width;

        X = screenX * 10f;
        Y = screenY * 10f;
        Z = 0;
        Update();
    }

    public MySprite DrawCursorVertex()
    {
        return new MySprite(SpriteType.TEXTURE, "Cirle", _vertexPosition, _vertexSize, _vertexCurrentColor, "", TextAlignment.CENTER, 0f);
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

    // Новый метод для рисования ребра с привязкой к курсору
    public MySprite DrawToCursor(float Width, Vector2 cursorPosition, int vertexToReplace)
    {
        Vector2 _vertex1Position, _vertex2Position;
        
        if (vertexToReplace == 1)
        {
            // Заменяем первую вершину на позицию курсора
            _vertex1Position = cursorPosition;
            // Вторая вершина: обычное преобразование
            _vertex2Position = new Vector2(256f + _vertex2.GetPosition().X * Width, 256f + _vertex2.GetPosition().Y * Width);
        }
        else // vertexToReplace == 2
        {
            // Первая вершина: обычное преобразование
            _vertex1Position = new Vector2(256f + _vertex1.GetPosition().X * Width, 256f + _vertex1.GetPosition().Y * Width);
            // Заменяем вторую вершину на позицию курсора
            _vertex2Position = cursorPosition;
        }
        
        Vector2 CenterVector = (_vertex1Position + _vertex2Position) / 2;
        return new MySprite(
            SpriteType.TEXTURE,
            "SquareSimple",
            CenterVector,
            new Vector2((_vertex1Position - _vertex2Position).Length(), _edgeWidth),
            Color.Orange,
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
        const float width = 10f;
        Vector2 v1 = GetVertex1ScreenPos(width);
        Vector2 v2 = GetVertex2ScreenPos(width);
        
        Vector2 edge = v2 - v1;
        float edgeLength = edge.Length();
        
        Vector2 edgeDir = edge / edgeLength;
        
        Vector2 toCursor = _cursorPosition - v1;
        
        float projection = Vector2.Dot(toCursor, edgeDir);
        
        projection = Math.Max(0, Math.Min(edgeLength, projection));
        
        Vector2 closestPoint = v1 + edgeDir * projection;
        
        float distance = Vector2.Distance(_cursorPosition, closestPoint);
        
        float threshold = Math.Max(_edgeWidth, 10f);
        
        return distance <= threshold;
    }

    public float GetDistanceToCursor(Vector2 cursorPos)
    {
        const float width = 10f;
        Vector2 v1 = GetVertex1ScreenPos(width);
        Vector2 v2 = GetVertex2ScreenPos(width);
        
        Vector2 edgeVec = v2 - v1;
        float edgeLength = edgeVec.Length();
        
        Vector2 edgeDir = edgeVec / edgeLength;
        
        Vector2 toCursor = cursorPos - v1;
        
        float projection = Vector2.Dot(toCursor, edgeDir);
        
        projection = Math.Max(0, Math.Min(edgeLength, projection));
        
        Vector2 closestPoint = v1 + edgeDir * projection;
        
        return Vector2.Distance(cursorPos, closestPoint);
    }

    public float GetAverageZ()
    {
        return (_vertex1.GetPosition3D().Z + _vertex2.GetPosition3D().Z) / 2f;
    }
}

class Button
{
    int id;
    string text;
    Color currentColor, defaultColor, focusColor;
    Vector2 position;

    public Button(int ID, string Text, Color Color, Vector2 Position)
    {
        this.id = ID;
        this.text = Text;
        this.defaultColor = Color;
        this.currentColor = Color.Black;
        this.focusColor = Color.Red;
        this.position = Position;
    }

    public Button(int ID, string Text, Color DefaultColor, Color FocusColor, Vector2 Position)
    {
        this.id = ID;
        this.text = Text;
        this.defaultColor = DefaultColor;
        this.currentColor = Color.Black;
        this.focusColor = FocusColor;
        this.position = Position;
    }

    public List<MySprite> Draw()
    {
        List<MySprite> spriteList = new List<MySprite>();
        spriteList.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", position, new Vector2(40f + text.Length * 12f, 50f), defaultColor, "", TextAlignment.CENTER, 0f));
        spriteList.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", position, new Vector2(36f + text.Length * 12f, 48f), currentColor, "", TextAlignment.CENTER, 0f));
        spriteList.Add(new MySprite(SpriteType.TEXT, text, new Vector2(position.X, position.Y - 15f), null, Color.White, "Debug", TextAlignment.CENTER, 1f));
        return spriteList;
    }

    public void ChangeColor(Color ChangedColor)
    {
        currentColor = ChangedColor;
    }

    public Color GetDefaultColor()
    {
        return defaultColor;
    }

    public Color GetFocusColor()
    {
        return focusColor;
    }

    public int GetID()
    {
        return id;
    }

    public bool IsHovered(Vector2 Position)
    {
        Vector2 size = new Vector2(40f + text.Length * 12f, 50f);
        if (Position.X >= position.X - size.X / 2 && Position.X <= position.X + size.X / 2 &&
            Position.Y >= (position.Y - size.Y / 2) - 512f && Position.Y <= (position.Y + size.Y / 2) - 512f) return true;
        else return false;
    }

    public void SetText(String Text)
    {
        this.text = Text;
    }

    public string GetText()
    {
        return text;
    }
}