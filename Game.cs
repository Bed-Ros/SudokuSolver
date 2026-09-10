using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace SudokuSolver
{
    //Классическое Судоку
    public partial class Game : ObservableObject
    {
        //Конструктор
        public Game(int height, int width)
        {
            //Заполняем размеры
            BlockHeight = height;
            BlockWidth = width;
            TotalDiameter = height * width;
            //Заполняем инициирующие данные
            InitPossibleValues();
            InitCreateGrid();
            InitCellsThickness();
        }

        //Размеры блоков
        public int BlockHeight { get; private set; }
        public int BlockWidth { get; private set; }

        //Количество блоков
        public int BlockRowsCount => BlockWidth;
        public int BlockColumnsCount => BlockHeight;

        //Размер всей сетки
        public int TotalDiameter { get; private set; }

        //Все возможные значения
        public List<int> PossibleValues { get; private set; } = [];

        //Автоматическая валидация
        [ObservableProperty]
        public partial bool AutoValidate { get; set; } = false;

        //Происходит ли сейчас решение
        [ObservableProperty]
        public partial bool IsSolving { get; set; } = false;

        //Сетка
        [ObservableProperty]
        public partial List<Cell> Cells { get; set; } = [];

        //Выбранная клетка
        [ObservableProperty]
        public partial Cell? SelectedCell { get; set; }

        //Сообщение пользователю о ходе решения
        [ObservableProperty]
        public partial string SolvingMessage { get; set; } = "";

        //Очередь клеток для проверки
        Queue<Cell> SolvingQueueToCheck = [];

        //Индекс следующего шага решения
        int NextSolvingStep = 0;

        //Решена ли игра
        public bool IsSolved
        {
            get
            {
                //Если не все клетки заполнены, игра не решена
                if (Cells.Count(x => x.Value != null) < Cells.Count) return false;
                //Если все клетки корректны, игра решена
                return Cells.All(x => CheckCell(x.Row, x.Column));
            }
        }

        //Заполняем возможные значения
        void InitPossibleValues()
        {
            PossibleValues.Clear();
            for (int i = 1; i <= TotalDiameter; i++)
            {
                PossibleValues.Add(i);
            }
        }

        //Возвращает возможные значения для клетки
        public List<int> CellPossibleValues(int row, int col)
        {
            var possibleValues = new HashSet<int>(PossibleValues);
            //Убираем значения из строки
            for (int c = 0; c < TotalDiameter; c++)
            {
                var value = Cells[Index(row, c)].Value;
                if (value != null)
                    possibleValues.Remove(value.Value);
            }
            //Убираем значения из столбца
            for (int r = 0; r < TotalDiameter; r++)
            {
                var value = Cells[Index(r, col)].Value;
                if (value != null)
                    possibleValues.Remove(value.Value);
            }
            // Убираем значения из блока
            for (int r = BlockStartRow(row); r < BlockEndRow(row); r++)
            {
                for (int c = BlockStartCol(col); c < BlockEndCol(col); c++)
                {
                    var value = Cells[Index(r, c)].Value;
                    if (value != null)
                        possibleValues.Remove(value.Value);
                }
            }
            return [.. possibleValues];
        }

        //Начальное заполнение сетки пустыми клетками
        void InitCreateGrid()
        {
            Cells.Clear();
            for (int i = 0; i < TotalDiameter * TotalDiameter; i++)
            {
                var (row, col) = Coords(i);
                var cell = new Cell(row, col);
                //Добавляем прослушку события изменения значения клетки для автоматической валидации
                cell.PropertyChanged += Cell_PropertyChanged;
                Cells.Add(cell);
            }
        }

        //Возвращаем сетку в начальное состояние
        public void ResetGrid()
        {
            Parallel.For(0, Cells.Count, i =>
            {
                Cells[i].Reset();
            });
        }

        //Заполняем сетку начальными данными
        public void FillGrid(int invariableCellsPercent)
        {
            //Убираем невозможные варианты
            invariableCellsPercent = MinMax(0, invariableCellsPercent, 100);
            //Если требуется заполнить 0% клеток, то просто сбрасываем сетку
            if (invariableCellsPercent == 0)
            {
                ResetGrid();
                return;
            }
            //Заполняем все клетки значениями по порядку,
            //смещаясь каждую строку на ширину блока и дополнительно смещаясь на одну клетку каждую строку блоков
            var valueIndex = 0;
            for (int row = 0; row < TotalDiameter; row++)
            {
                if (row != 0 && row % BlockHeight == 0)
                    valueIndex++;
                for (int col = 0; col < TotalDiameter; col++)
                {
                    Cells[Index(row, col)].Value = PossibleValues[valueIndex % PossibleValues.Count];
                    valueIndex++;
                }
                valueIndex += BlockWidth;
            }
            //Выполняем случайные преобразования
            var rand = new Random();
            for (int i = 0; i < TotalDiameter * TotalDiameter; i++)
            {
                bool success = false;
                switch (rand.Next(8))
                {
                    //Поменять местами две строки внутри одной строки блоков
                    case 0:
                        var blockRow = rand.Next(0, BlockRowsCount);
                        var row1 = rand.Next(0, BlockHeight);
                        var row2 = rand.Next(0, BlockHeight);
                        success = SwapRows(blockRow * BlockHeight + row1, blockRow * BlockHeight + row2);
                        break;
                    //Поменять местами два столбца внутри одного столбца блоков
                    case 1:
                        var blockColumn = rand.Next(0, BlockColumnsCount);
                        var column1 = rand.Next(0, BlockWidth);
                        var column2 = rand.Next(0, BlockWidth);
                        success = SwapColumns(blockColumn * BlockWidth + column1, blockColumn * BlockWidth + column2);
                        break;
                    //Меняем местами два столба блоков
                    case 2:
                        success = SwapBlockCols(rand.Next(0, BlockColumnsCount), rand.Next(0, BlockColumnsCount));
                        break;
                    //Меняем местами две строки блоков
                    case 3:
                        success = SwapBlockRows(rand.Next(0, BlockRowsCount), rand.Next(0, BlockRowsCount));
                        break;
                    //Отражаем сетку по основной диагонали
                    case 4:
                        success = DiagonalReflectGrid1();
                        break;
                    //Отражаем сетку по второстепенной диагонали
                    case 5:
                        success = DiagonalReflectGrid2();
                        break;
                    //Отражаем сетку по вертикали 
                    case 6:
                        VerticalReflectGrid();
                        success = true;
                        break;
                    //Отражаем сетку по горизонтали 
                    case 7:
                        HorizontalReflectGrid();
                        success = true;
                        break;
                }
                if (!success)
                    i--;
            }
            //Удаляем значения случайныx клеток
            //Делаем оставшиеся клетки неизменяемыми
            //Если требуется заполнить 100% клеток, то оставляем сетку заполненной и делаем все клетки неизменяемыми
            for (int i = 0; i < Cells.Count; i++)
            {
                if (invariableCellsPercent == 100 || rand.Next(0, 100) < invariableCellsPercent)
                    Cells[i].Variable = false;
                else
                    Cells[i].Value = null;
            }
        }

        //Событие изменения значения клетки
        private void Cell_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is Cell cell &&
                e.PropertyName == nameof(Cell.Value))
            {
                if (AutoValidate)
                    ValidateRelatedCells(cell);
                OnPropertyChanged(nameof(IsSolved));
            }
        }

        //Отражаем сетку по главной диагонали (если сетка квадратная)
        bool DiagonalReflectGrid1()
        {
            if (BlockHeight != BlockWidth) return false;
            for (int row = 0; row < TotalDiameter; row++)
            {
                for (int col = row; col < TotalDiameter; col++)
                {
                    SwapCellValues(row, col, col, row);
                }
            }
            return true;
        }

        //Отражаем сетку по второстепенной диагонали (если сетка квадратная)
        bool DiagonalReflectGrid2()
        {
            if (BlockHeight != BlockWidth) return false;
            for (int row = 0; row < TotalDiameter; row++)
            {
                for (int col = 0; col < TotalDiameter - row; col++)
                {
                    SwapCellValues(row, col, TotalDiameter - col - 1, TotalDiameter - row - 1);
                }
            }
            return true;
        }

        //Отражаем сетку по вертикали
        void VerticalReflectGrid()
        {
            Parallel.For(0, TotalDiameter / 2, row =>
            {
                SwapRows(row, TotalDiameter - row - 1);
            });
        }

        //Отражаем сетку по горизонтали
        void HorizontalReflectGrid()
        {
            Parallel.For(0, TotalDiameter / 2, col =>
            {
                SwapColumns(col, TotalDiameter - col - 1);
            });
        }

        //Возвращает значение, ограниченное минимальным и максимальным значением
        static int MinMax(int min, int value, int max)
        {
            return Math.Max(min, Math.Min(value, max));
        }

        //Меняет местами две строки блоков
        bool SwapBlockRows(int blockRow1, int blockRow2)
        {
            if (blockRow1 == blockRow2) return false;
            blockRow1 = MinMax(0, blockRow1, BlockRowsCount - 1);
            blockRow2 = MinMax(0, blockRow2, BlockRowsCount - 1);
            Parallel.For(0, BlockHeight, (i, _) =>
            {
                SwapRows(blockRow1 * BlockHeight + i, blockRow2 * BlockHeight + i);
            });
            return true;
        }

        //Меняет местами два столбца блоков
        bool SwapBlockCols(int blockCol1, int blockCol2)
        {
            if (blockCol1 == blockCol2) return false;
            blockCol1 = MinMax(0, blockCol1, BlockColumnsCount - 1);
            blockCol2 = MinMax(0, blockCol2, BlockColumnsCount - 1);
            Parallel.For(0, BlockWidth, (i, _) =>
            {
                SwapColumns(blockCol1 * BlockWidth + i, blockCol2 * BlockWidth + i);
            });
            return true;
        }

        //Меняет местами две строки (стоит менять только строки внутри одной строки блоков)
        bool SwapRows(int row1, int row2)
        {
            if (row1 == row2) return false;
            Parallel.For(0, TotalDiameter, (col, _) =>
            {
                SwapCellValues(row1, col, row2, col);
            });
            return true;
        }

        //Меняет местами два столбца (стоит менять только столбцы внутри одного столбца блоков)
        bool SwapColumns(int col1, int col2)
        {
            if (col1 == col2) return false;
            Parallel.For(0, TotalDiameter, (row, _) =>
            {
                SwapCellValues(row, col1, row, col2);
            });
            return true;
        }

        //Меняет местами два значения в клетках
        void SwapCellValues(int row1, int col1, int row2, int col2)
        {
            if (row1 == row2 && col1 == col2) return;
            int index1 = Index(row1, col1);
            int index2 = Index(row2, col2);
            (Cells[index1].Value, Cells[index2].Value) = (Cells[index2].Value, Cells[index1].Value);
        }

        //Убирает все ошибочные значения
        public void RemoveErrorValues()
        {
            for (int i = 0; i < Cells.Count; i++)
            {
                if (Cells[i].IsError)
                    Cells[i].Value = null;
            }
        }

        //Запускает последовательность действий для решения судоку
        public void Solve()
        {
            if (!IsSolving)
                IsSolving = true;
            //План: Поочередно проверяем все клетки на наличие единственного возможного значения и заполняем их
            switch (NextSolvingStep)
            {
                case 0:
                    //Убираем ошибочные значения, если имеются
                    SolvingMessage = "Убираем ошибочные значения, если имеются";
                    ValidateAllCells();
                    RemoveErrorValues();
                    //Переходим к следующему шагу
                    NextSolvingStep++;
                    break;
                case 1:
                    //Находим для всех пустых клеток их возможные значения
                    SolvingMessage = "Находим возможные значения для пустых клеток";
                    Parallel.ForEach(Cells, cell =>
                    {
                        if (cell.Value == null)
                            cell.PossibleValues = [.. CellPossibleValues(cell.Row, cell.Column)];
                    });
                    //Создаем очередь из пустых клеток для проверки
                    SolvingQueueToCheck = new Queue<Cell>(Cells.Where(x => x.Value == null));
                    NextSolvingStep++;
                    break;
                case 2:
                    //Проверяем, что все клетки заполнены
                    if (IsSolved)
                    {
                        SolvingMessage = "Судоку решен успешно";
                        NextSolvingStep++;
                        break;
                    }
                    //Берем клетки из очереди пока не найдем клетку с пустым значением или пока не закончатся клетки в очереди
                    Cell? cell = null;
                    while ( SolvingQueueToCheck.TryDequeue(out var c))
                    {
                        if (c != null && c.Value == null)
                        {
                            cell = c;
                            break;
                        }
                    }
                    //Если в очереди нет клеток, то пишем результат и переходим к следующему шагу
                    if (cell == null)
                    {
                        SolvingMessage = "Не удалось решить Судоку";
                        NextSolvingStep++;
                        break;
                    }
                    //Показываем пользователю текущую клетку
                    SelectCell(cell);
                    //Если для клетки найдено только одно возможное значение
                    if (cell.PossibleValues.Count == 1)
                    {
                        SolvingMessage = "Вводим единственное возможное значение для выбранной клетки и переходим к следующей";
                        //Заполняем клетку этим значением
                        cell.Value = cell.PossibleValues.First();
                        //Убираем это значение из возможных значений для связанных клеток и добавляем эту клетку в очередь 
                        foreach (var relatedCell in RelatedCells(cell).Where(c => c.Value == null))
                        {
                            relatedCell.PossibleValues.Remove(cell.Value.Value);
                            SolvingQueueToCheck.Enqueue(relatedCell);
                        }
                    }
                    //Если для клетки не найдено возможных значений, то Судоку не имеет решения
                    else if (cell.PossibleValues.Count == 0)
                    {
                        SolvingMessage = "Для данной клетки нет возможных значений. Судоку не имеет решения";
                        NextSolvingStep++;
                    }
                    else
                        SolvingMessage = "Выбранная клетка имеет несколько возможных значений. Переходим к следующей";
                    break;
                default:
                    //Если дошли до конца последовательности, то сбрасываем шаги и останавливаем решение
                    NextSolvingStep = 0;
                    IsSolving = false;
                    break;
            }
        }

        //Получить связанные клетки
        public IEnumerable<Cell> RelatedCells(Cell cell)
        {
            //Собираем информауию о блоке
            var blockStartRow = BlockStartRow(cell.Row);
            var blockEndRow = BlockEndRow(cell.Row);
            var blockStartCol = BlockStartCol(cell.Column);
            var blockEndCol = BlockEndCol(cell.Column);
            //Блок
            for (int row = blockStartRow; row < blockEndRow; row++)
            {
                for (int col = blockStartCol; col < blockEndCol; col++)
                {
                    if (row != cell.Row || col != cell.Column)
                        yield return Cells[Index(row, col)];
                }
            }
            //Строка
            for (int row = 0; row < TotalDiameter; row++)
            {
                if (row < blockStartRow || row >= blockEndRow)
                    yield return Cells[Index(row, cell.Column)];
            }
            //Столбец
            for (int col = 0; col < TotalDiameter; col++)
            {
                if (col < blockStartCol || col >= blockEndCol)
                    yield return Cells[Index(cell.Row, col)];
            }

        }

        //Валидация всех клеток
        public void ValidateAllCells()
        {
            for (int row = 0; row < TotalDiameter; row++)
            {
                for (int col = 0; col < TotalDiameter; col++)
                {
                    ValidateCell(row, col);
                }
            }
        }

        //Валидация связанных клеток
        public void ValidateRelatedCells(Cell cell)
        {
            foreach (var relatedCell in RelatedCells(cell))
            {
                ValidateCell(relatedCell.Row, relatedCell.Column);
            }
        }

        //Проверка, что все значения корректны
        public bool CheckAllCells()
        {
            for (int row = 0; row < TotalDiameter; row++)
            {
                for (int col = 0; col < TotalDiameter; col++)
                {
                    if (!CheckCell(row, col))
                        return false;
                }
            }
            return true;
        }

        //Валидация клетки
        public void ValidateCell(int row, int col)
        {
            Cells[Index(row, col)].IsError = !CheckCell(row, col);
        }

        //Проверка, что значение в клетке корректно
        public bool CheckCell(int row, int col)
        {
            var index = Index(row, col);
            //null - всегда корректно
            if (Cells[index].Value == null) return true;
            return CheckCellByRow(row, col) && CheckCellByColumn(row, col) && CheckCellByBlock(row, col);
        }

        //Проверка клетки в строке
        public bool CheckCellByRow(int row, int col)
        {
            var value = Cells[Index(row, col)].Value;
            //null - всегда корректно
            if (value == null) return true;
            for (int c = 0; c < TotalDiameter; c++)
            {
                if (c != col && Cells[Index(row, c)].Value == value)
                    return false;
            }
            return true;
        }

        //Проверка клетки в столбце
        public bool CheckCellByColumn(int row, int col)
        {
            var value = Cells[Index(row, col)].Value;
            //null - всегда корректно
            if (value == null) return true;
            for (int r = 0; r < TotalDiameter; r++)
            {
                if (r != row && Cells[Index(r, col)].Value == value)
                    return false;
            }
            return true;
        }

        //Проверка клетки в блоке
        public bool CheckCellByBlock(int row, int col)
        {
            var value = Cells[Index(row, col)].Value;
            //null - всегда корректно
            if (value == null) return true;
            for (int r = BlockStartRow(row); r < BlockEndRow(row); r++)
            {
                for (int c = BlockStartCol(col); c < BlockEndCol(col); c++)
                {
                    if ((r != row || c != col) && Cells[Index(r, c)].Value == value)
                        return false;
                }
            }
            return true;
        }

        //Начало блока строка
        int BlockStartRow(int cellRow) => (cellRow / BlockHeight) * BlockHeight;

        //Начало блока колонка
        int BlockStartCol(int cellCol) => (cellCol / BlockWidth) * BlockWidth;

        //Конец блока строка
        int BlockEndRow(int cellRow) => BlockStartRow(cellRow) + BlockHeight;

        //Конец блока колонка
        int BlockEndCol(int cellCol) => BlockStartCol(cellCol) + BlockWidth;

        //Логика выбора ячейки
        [RelayCommand]
        private void SelectCell(Cell cell)
        {
            if (cell == null) return;

            // Снимаем выделение с предыдущей ячейки, если она была
            SelectedCell?.IsSelected = false;

            // Запоминаем новую ячейку и активируем её подсветку
            SelectedCell = cell;
            SelectedCell.IsSelected = true;
        }

        //Логика ввода значения в ячейку
        [RelayCommand]
        private void SetSelectedCell(int? value)
        {
            if (SelectedCell == null) return;
            SelectedCell.Value = value;
        }

        //Рассчитывает толщину границ
        void InitCellsThickness()
        {
            for (int i = 0; i < Cells.Count; i++)
            {
                InitCellThickness(i);
            }
        }
        async Task InitCellsThicknessAsync()
        {
            await Parallel.ForAsync(0, Cells.Count, async (i, _) =>
            {
                InitCellThickness(i);
            });
        }
        void InitCellThickness(int index)
        {
            //Базовая тонкая граница
            double right = 1;
            double bottom = 1;

            //Если это последний столбец — не делаем границу
            if (Cells[index].Column == TotalDiameter - 1)
                right = 0;
            //Если это правая граница блока — делаем её ЖИРНОЙ
            else if ((Cells[index].Column + 1) % BlockWidth == 0)
                right = 3;

            //Если это последняя строка — не делаем границу
            if (Cells[index].Row == TotalDiameter - 1)
                bottom = 0;
            //Если это нижняя граница блока — делаем её ЖИРНОЙ
            else if ((Cells[index].Row + 1) % BlockHeight == 0)
                bottom = 3;

            Cells[index].BorderThickness = new Thickness(0, 0, right, bottom);
        }

        //Возвращает индекс клетки в массиве по её координатам
        public int Index(int row, int col)
        {
            return row * TotalDiameter + col;
        }

        //Возвращает координаты клетки по её индексу в массиве
        public (int row, int col) Coords(int index)
        {
            return (index / TotalDiameter, index % TotalDiameter);
        }
    }
}
