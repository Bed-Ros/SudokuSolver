using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;

namespace SudokuSolver
{
    //Классическое Судоку
    public partial class Game : ObservableObject
    {
        //Размеры блоков
        public int Height { get; private set; }
        public int Width { get; private set; }

        //Размер всей сетки
        public int TotalDiameter { get; private set; }

        //Все возможные значения
        public List<int> PossibleValues { get; private set; }

        //Сетка
        [ObservableProperty]
        public partial ObservableCollection<Cell> Cells { get; set; } = [];

        //Выбранная клетка
        [ObservableProperty]
        public partial Cell? SelectedCell { get; set; }

        //Конструктор
        public Game(int height, int width, int invariableCells = 0)
        {
            //Заполняем размеры
            Height = height;
            Width = width;
            TotalDiameter = Height * Width;

            //Заполняем возможные значения
            PossibleValues = [];
            for (int i = 1; i <= TotalDiameter; i++)
            {
                PossibleValues.Add(i);
            }

            FillGrid(invariableCells);

            SetCellsThickness();
        }

        //Заполняем сетку начальными данными
        void FillGrid(int invariableCells)
        {
            //Заполняем сетку пустыми ячейками
            Cells.Clear();
            for (int i = 0; i < TotalDiameter * TotalDiameter; i++)
            {
                Cells.Add(new());
            }
            //Убираем невозможные варианты
            invariableCells = Math.Max(0, Math.Min(invariableCells, TotalDiameter * TotalDiameter));
            //Заполняем случайные клетки начальными значениями
            var rand = new Random();
            while (invariableCells > 0)
            {
                int row = rand.Next(0, TotalDiameter);
                int col = rand.Next(0, TotalDiameter);
                int index = Index(row, col);
                if (Cells[index].Value == null)
                {
                    Cells[index].Value = PossibleValues[rand.Next(PossibleValues.Count)];
                    if (CheckCell(row, col))
                    {
                        Cells[index].Variable = false;
                        invariableCells--;
                    }
                    else
                    {
                        Cells[index].Value = null;
                    }
                }
            }
        }

        //Решение
        public void Solve()
        {
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
            var value = Cells[row * TotalDiameter + col].Value;
            //null - всегда корректно
            if (value == null) return true;
            //Проверяем строку
            for (int c = 0; c < TotalDiameter; c++)
            {
                if (c != col && Cells[row * TotalDiameter + c].Value == value)
                    return false;
            }
            //Проверяем столбец
            for (int r = 0; r < TotalDiameter; r++)
            {
                if (r != row && Cells[r * TotalDiameter + col].Value == value)
                    return false;
            }
            //Проверяем блок
            int blockRowStart = (row / Height) * Height;
            int blockColStart = (col / Width) * Width;
            for (int r = blockRowStart; r < blockRowStart + Height; r++)
            {
                for (int c = blockColStart; c < blockColStart + Width; c++)
                {
                    if ((r != row || c != col) && Cells[r * TotalDiameter + c].Value == value)
                        return false;
                }
            }
            return true;
        }

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
        private void SetSelectedValue(int value)
        {
            if (SelectedCell == null) return;
            SelectedCell.Value = value;
        }

        //Рассчитывает толщину границ
        void SetCellsThickness()
        {
            for (int row = 0; row < TotalDiameter; row++)
            {
                for (int col = 0; col < TotalDiameter; col++)
                {
                    int index = Index(row, col);

                    // Базовая тонкая граница
                    double right = 1;
                    double bottom = 1;

                    // Если это последний столбец — не делаем границу
                    if (col == TotalDiameter - 1)
                        right = 0;
                    // Если это правая граница блока — делаем её ЖИРНОЙ
                    else if ((col + 1) % Width == 0)
                        right = 3;

                    // Если это последняя строка — не делаем границу
                    if (row == TotalDiameter - 1)
                        bottom = 0;
                    // Если это нижняя граница блока — делаем её ЖИРНОЙ
                    else if ((row + 1) % Height == 0)
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
