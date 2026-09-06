using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;

namespace SudokuSolver
{
    //Классическое Судоку
    public partial class Game : ObservableObject
    {
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

        //Сетка
        [ObservableProperty]
        public partial ObservableCollection<Cell> Cells { get; set; } = [];

        //Выбранная клетка
        [ObservableProperty]
        public partial Cell? SelectedCell { get; set; }

        //Конструктор
        public Game(int height, int width, int invariableCellsPercent = 0)
        {
            //Заполняем размеры
            BlockHeight = height;
            BlockWidth = width;
            TotalDiameter = height * width;

            InitPossibleValues();
            InitFillGrid(invariableCellsPercent);
            InitCellsThickness();
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

        //Заполняем сетку начальными данными
        void InitFillGrid(int invariableCellsPercent)
        {
            //Заполняем все клетки значениями по порядку,
            //смещаясь каждую строку на ширину блока и дополнительно смещаясь на одну клетку каждую строку блоков
            Cells.Clear();
            var valueIndex = 0;
            for (int row = 0; row < TotalDiameter; row++)
            {
                if (row != 0 && row % BlockHeight == 0)
                    valueIndex++;
                for (int col = 0; col < TotalDiameter; col++)
                {
                    Cells.Add(new(row, col, PossibleValues[valueIndex % PossibleValues.Count]));
                    valueIndex++;
                }
                valueIndex += BlockWidth;
            }
            //Случайное количество раз выполняем случайные преобразования, чтобы перемешать сетку
            var rand = new Random();
            for (int i = 0; i < rand.Next(TotalDiameter * TotalDiameter, TotalDiameter * TotalDiameter * TotalDiameter); i++)
            {
                switch (rand.Next(6))
                {
                    //Поменять местами две строки внутри одной строки блоков
                    case 0:
                        var blockRow = rand.Next(0, BlockRowsCount);
                        var row1 = rand.Next(0, BlockHeight);
                        var row2 = rand.Next(0, BlockHeight);
                        SwapRows(blockRow * BlockHeight + row1, blockRow * BlockHeight + row2);
                        break;
                    //Поменять местами два столбца внутри одного столбца блоков
                    case 1:
                        var blockColumn = rand.Next(0, BlockColumnsCount);
                        var column1 = rand.Next(0, BlockWidth);
                        var column2 = rand.Next(0, BlockWidth);
                        SwapColumns(blockColumn * BlockWidth + column1, blockColumn * BlockWidth + column2);
                        break;
                    //Меняем местами два столба блоков
                    case 2:
                        SwapBlockCols(rand.Next(0, BlockColumnsCount), rand.Next(0, BlockColumnsCount));
                        break;
                    //Меняем местами две строки блоков
                    case 3:
                        SwapBlockRows(rand.Next(0, BlockRowsCount), rand.Next(0, BlockRowsCount));
                        break;
                    //Отражаем сетку по диагонали слева сверху направо вниз
                    case 4:
                        ReflectGrid1();
                        break;
                    //Отражаем сетку по диагонали слева снизу направо вверх
                    case 5:
                        ReflectGrid2();
                        break;
                }
            }
            //Убираем невозможные варианты
            invariableCellsPercent = MinMax(0, invariableCellsPercent, 100);
            int invariableCells = (int)(TotalDiameter * TotalDiameter * invariableCellsPercent / 100.0);
            //Удаляем значения случайныx клеток
            int n = 0;
            while (n < TotalDiameter * TotalDiameter - invariableCells)
            {
                int index = rand.Next(0, TotalDiameter * TotalDiameter);
                if (Cells[index].Value != null)
                {
                    Cells[index].Value = null;
                    n++;
                }
            }
            //Делаем оставшиеся клетки неизменяемыми
            //Добавляем прослушку события изменения значения клетки для автоматической валидации
            for (int i = 0; i < Cells.Count; i++)
            {
                if (Cells[i].Value != null)
                    Cells[i].Variable = false;
                Cells[i].PropertyChanged += Cell_PropertyChanged;
            }
        }

        //Событие изменения значения клетки
        private void Cell_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (!AutoValidate) return;
            if (sender is not Cell cell) return;
            if (e.PropertyName != nameof(Cell.Value)) return;
            ValidateCell(cell.Row, cell.Column);
        }

        //Отражаем сетку по главной диагонали (если сетка квадратная)
        void ReflectGrid1()
        {
            if (BlockHeight != BlockWidth) return;
            for (int row = 0; row < TotalDiameter; row++)
            {
                for (int col = row; col < TotalDiameter; col++)
                {
                    SwapCellValues(row, col, col, row);
                }
            }
        }

        //Отражаем сетку по второстепенной диагонали (если сетка квадратная)
        void ReflectGrid2()
        {
            if (BlockHeight != BlockWidth) return;
            for (int row = 0; row < TotalDiameter; row++)
            {
                for (int col = 0; col < TotalDiameter - row; col++)
                {
                    SwapCellValues(row, col, TotalDiameter - col - 1, TotalDiameter - row - 1);
                }
            }
        }

        //Возвращает значение, ограниченное минимальным и максимальным значением
        static int MinMax(int min, int value, int max)
        {
            return Math.Max(min, Math.Min(value, max));
        }

        //Меняет местами две строки блоков
        void SwapBlockRows(int blockRow1, int blockRow2)
        {
            if (blockRow1 == blockRow2) return;
            blockRow1 = MinMax(0, blockRow1, BlockRowsCount - 1);
            blockRow2 = MinMax(0, blockRow2, BlockRowsCount - 1);
            for (int i = 0; i < BlockHeight; i++)
            {
                SwapRows(blockRow1 * BlockHeight + i, blockRow2 * BlockHeight + i);
            }
        }

        //Меняет местами два столбца блоков
        void SwapBlockCols(int blockCol1, int blockCol2)
        {
            if (blockCol1 == blockCol2) return;
            blockCol1 = MinMax(0, blockCol1, BlockColumnsCount - 1);
            blockCol2 = MinMax(0, blockCol2, BlockColumnsCount - 1);
            for (int i = 0; i < BlockWidth; i++)
            {
                SwapColumns(blockCol1 * BlockWidth + i, blockCol2 * BlockWidth + i);
            }
        }

        //Меняет местами две строки (стоит менять только строки внутри одной строки блоков)
        void SwapRows(int row1, int row2)
        {
            if (row1 == row2) return;
            for (int col = 0; col < TotalDiameter; col++)
            {
                SwapCellValues(row1, col, row2, col);
            }
        }

        //Меняет местами два столбца (стоит менять только столбцы внутри одного столбца блоков)
        void SwapColumns(int col1, int col2)
        {
            if (col1 == col2) return;
            for (int row = 0; row < TotalDiameter; row++)
            {
                SwapCellValues(row, col1, row, col2);
            }
        }

        //Меняет местами два значения в клетках
        void SwapCellValues(int row1, int col1, int row2, int col2)
        {
            if (row1 == row2 && col1 == col2) return;
            int index1 = Index(row1, col1);
            int index2 = Index(row2, col2);
            (Cells[index1].Value, Cells[index2].Value) = (Cells[index2].Value, Cells[index1].Value);
        }

        //Убирает все ошибочный значения
        public void RemoveErrorValues()
        {
            for (int i = 0; i < Cells.Count; i++)
            {
                if (Cells[i].IsError)
                    Cells[i].Value = null;
            }
        }

        //Решение
        public void Solve()
        {
            RemoveErrorValues();
            //TODO
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
            //Неизменяемые клетки всегда корректны
            if (!Cells[index].Variable) return true;
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
            for (int row = 0; row < TotalDiameter; row++)
            {
                for (int col = 0; col < TotalDiameter; col++)
                {
                    int index = Index(row, col);

                    //Базовая тонкая граница
                    double right = 1;
                    double bottom = 1;

                    //Если это последний столбец — не делаем границу
                    if (col == TotalDiameter - 1)
                        right = 0;
                    //Если это правая граница блока — делаем её ЖИРНОЙ
                    else if ((col + 1) % BlockWidth == 0)
                        right = 3;

                    //Если это последняя строка — не делаем границу
                    if (row == TotalDiameter - 1)
                        bottom = 0;
                    //Если это нижняя граница блока — делаем её ЖИРНОЙ
                    else if ((row + 1) % BlockHeight == 0)
                        bottom = 3;

                    Cells[index].BorderThickness = new Thickness(0, 0, right, bottom);
                }
            }
        }

        //Возвращает индекс клетки в массиве по её координатам
        public int Index(int row, int col)
        {
            return row * TotalDiameter + col;
        }
    }
}
