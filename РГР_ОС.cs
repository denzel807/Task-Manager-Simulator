using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ProcessScheduler
{
    public enum TaskState
    {
        Waiting,
        Ready,
        Running,
        Completed
    }

    public class Task
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int BurstTime { get; set; }
        public int RemainingTime { get; set; }
        public TaskState State { get; set; }
        public int QuantumUsed { get; set; }
        public Color Color { get; set; }
        public DateTime CompletionTime { get; set; }
    }

    public partial class MainForm : Form
    {
        private List<Task> allTasks = new List<Task>();
        private Queue<Task> readyQueue = new Queue<Task>();
        private Task currentTask = null;
        private int currentTime = 0;
        private int timeQuantum = 10;
        private int nextTaskId = 1;
        private Timer timer;
        private Random random = new Random();
        private List<string> completedTasksReport = new List<string>();

        // Элементы управления
        private TextBox taskNameTextBox;
        private NumericUpDown burstTimeNumeric;
        private Button addTaskButton;
        private ListBox taskListBox;
        private ListBox completedListBox;
        private NumericUpDown quantumNumeric;
        private Button autoButton;
        private Button resetButton;
        private Panel progressPanel;
        private Label currentTimeLabel;
        private Label statusLabel;

        public MainForm()
        {
            InitializeComponents();
            UpdateDisplay();
        }

        private void InitializeComponents()
        {
            // Настройка главной формы
            this.Text = "Имитатор диспетчера ОС";
            this.Size = new Size(1000, 700);
            this.BackColor = Color.FromArgb(240, 240, 240);
            this.Font = new Font("Segoe UI", 9);

            // Левая панель - добавление задачи
            var leftPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 300,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };

            // Заголовок "Имитатор диспетчера ОС"
            var titleLabel = new Label
            {
                Text = "Имитатор диспетчера ОС",
                Dock = DockStyle.Top,
                Height = 60,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White
            };

            // Панель добавления задачи
            var addTaskPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 150,
                BackColor = Color.FromArgb(37, 37, 40),
                Padding = new Padding(20, 10, 20, 10)
            };

            var taskNameLabel = new Label
            {
                Text = "Имя задачи",
                Location = new Point(0, 10),
                Size = new Size(260, 20),
                ForeColor = Color.LightGray
            };

            taskNameTextBox = new TextBox
            {
                Location = new Point(0, 30),
                Size = new Size(260, 25),
                BackColor = Color.FromArgb(63, 63, 70),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = "" // Пустое поле
            };

            var burstTimeLabel = new Label
            {
                Text = "Время выполнения",
                Location = new Point(0, 65),
                Size = new Size(260, 20),
                ForeColor = Color.LightGray
            };

            burstTimeNumeric = new NumericUpDown
            {
                Location = new Point(0, 85),
                Size = new Size(260, 25),
                Minimum = 1,
                Maximum = 100,
                Value = 1, // Минимальное значение вместо 40
                BackColor = Color.FromArgb(63, 63, 70),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            addTaskButton = new Button
            {
                Text = "Добавить в список",
                Location = new Point(0, 115),
                Size = new Size(260, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            addTaskButton.Click += AddTaskButton_Click;

            addTaskPanel.Controls.AddRange(new Control[]
            {
                taskNameLabel, taskNameTextBox,
                burstTimeLabel, burstTimeNumeric,
                addTaskButton
            });

            // Панель списка задач
            var taskListPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30),
                Padding = new Padding(20, 10, 20, 10)
            };

            var taskListLabel = new Label
            {
                Text = "Список задач",
                Dock = DockStyle.Top,
                Height = 30,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };

            taskListBox = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10)
            };

            taskListPanel.Controls.Add(taskListBox);
            taskListPanel.Controls.Add(taskListLabel);

            leftPanel.Controls.Add(taskListPanel);
            leftPanel.Controls.Add(addTaskPanel);
            leftPanel.Controls.Add(titleLabel);

            // Центральная панель - прогресс и отчеты
            var centerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(20)
            };

            // Текущее время
            currentTimeLabel = new Label
            {
                Text = $"Текущее время: {currentTime}",
                Location = new Point(20, 20),
                Size = new Size(300, 30),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215)
            };

            // Прогресс выполнения
            var progressLabel = new Label
            {
                Text = "Прогресс выполнения",
                Location = new Point(20, 60),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(64, 64, 64)
            };

            progressPanel = new Panel
            {
                Location = new Point(20, 90),
                Size = new Size(400, 200),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };

            // Отчеты
            var reportsLabel = new Label
            {
                Text = "Отчеты",
                Location = new Point(20, 310),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(64, 64, 64)
            };

            var reportsSubLabel = new Label
            {
                Text = "Список выполненных задач",
                Location = new Point(20, 340),
                Size = new Size(300, 20),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray
            };

            completedListBox = new ListBox
            {
                Location = new Point(20, 370),
                Size = new Size(400, 200),
                BackColor = Color.White,
                ForeColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9)
            };

            centerPanel.Controls.AddRange(new Control[]
            {
                currentTimeLabel, progressLabel, progressPanel,
                reportsLabel, reportsSubLabel, completedListBox
            });

            // Правая панель - управление
            var rightPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 250,
                BackColor = Color.FromArgb(248, 248, 248),
                Padding = new Padding(20)
            };

            var quantumLabel = new Label
            {
                Text = "Квант времени",
                Location = new Point(0, 20),
                Size = new Size(210, 25),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(64, 64, 64),
                TextAlign = ContentAlignment.MiddleCenter
            };

            quantumNumeric = new NumericUpDown
            {
                Location = new Point(40, 50),
                Size = new Size(130, 30),
                Minimum = 1,
                Maximum = 100,
                Value = timeQuantum,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Center,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            quantumNumeric.ValueChanged += (s, e) => timeQuantum = (int)quantumNumeric.Value;

            autoButton = new Button
            {
                Text = "Автоматический",
                Location = new Point(20, 100),
                Size = new Size(170, 40),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            autoButton.Click += AutoButton_Click;

            resetButton = new Button
            {
                Text = "Сброс",
                Location = new Point(20, 160),
                Size = new Size(170, 40),
                BackColor = Color.FromArgb(244, 67, 54),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            resetButton.Click += ResetButton_Click;

            statusLabel = new Label
            {
                Text = "Добавьте задачи",
                Location = new Point(0, 220),
                Size = new Size(210, 30),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleCenter
            };

            rightPanel.Controls.AddRange(new Control[]
            {
                quantumLabel, quantumNumeric,
                autoButton, resetButton, statusLabel
            });

            // Добавление панелей на форму
            this.Controls.Add(centerPanel);
            this.Controls.Add(rightPanel);
            this.Controls.Add(leftPanel);

            // Таймер
            timer = new Timer();
            timer.Interval = 500;
            timer.Tick += Timer_Tick;
        }

        private void AddTaskButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(taskNameTextBox.Text))
            {
                MessageBox.Show("Введите имя задачи", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                taskNameTextBox.Focus();
                return;
            }

            AddTask(taskNameTextBox.Text, (int)burstTimeNumeric.Value);

            // Очищаем поля после добавления
            taskNameTextBox.Text = "";
            taskNameTextBox.Focus();
            burstTimeNumeric.Value = 1;
        }

        private void AddTask(string name, int burstTime)
        {
            var task = new Task
            {
                Id = nextTaskId++,
                Name = name,
                BurstTime = burstTime,
                RemainingTime = burstTime,
                State = TaskState.Ready,
                QuantumUsed = 0,
                Color = GetRandomColor()
            };

            allTasks.Add(task);
            readyQueue.Enqueue(task);

            UpdateDisplay();
            UpdateStatus($"Добавлена задача: {name}");
        }

        private Color GetRandomColor()
        {
            // Генерация цветов как на изображении
            int[] colors = { 0x2E7D32, 0x1565C0, 0x6A1B9A, 0xC62828, 0xEF6C00, 0x00838F, 0x283593, 0xAD1457 };
            return Color.FromArgb(colors[random.Next(colors.Length)]);
        }

        private void UpdateDisplay()
        {
            currentTimeLabel.Text = $"Текущее время: {currentTime}";

            // Обновление списка задач
            taskListBox.Items.Clear();
            foreach (var task in allTasks.Where(t => t.State != TaskState.Completed))
            {
                string status = task.State == TaskState.Running ? "⚡ " : "";
                int progressPercent = task.BurstTime > 0 ?
                    (int)((float)(task.BurstTime - task.RemainingTime) / task.BurstTime * 100) : 0;
                taskListBox.Items.Add($"• {status}{task.Name} - {task.RemainingTime}ед. ({progressPercent}%)");
            }

            // Обновление прогресса
            UpdateProgressDisplay();

            // Обновление отчетов
            completedListBox.Items.Clear();
            foreach (var report in completedTasksReport)
            {
                completedListBox.Items.Add(report);
            }

            // Обновление статуса
            UpdateStatusDisplay();
        }

        private void UpdateProgressDisplay()
        {
            progressPanel.Controls.Clear();

            if (allTasks.Count == 0)
            {
                var label = new Label
                {
                    Text = "Нет активных задач",
                    Location = new Point(10, 10),
                    Size = new Size(380, 30),
                    Font = new Font("Segoe UI", 10),
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Color.Gray
                };
                progressPanel.Controls.Add(label);
                return;
            }

            int y = 10;
            int barWidth = 350;

            foreach (var task in allTasks.Where(t => t.State != TaskState.Completed))
            {
                // Название задачи
                var nameLabel = new Label
                {
                    Text = task.Name,
                    Location = new Point(10, y),
                    Size = new Size(100, 20),
                    Font = new Font("Segoe UI", 9)
                };

                // Процент выполнения
                float progress = task.BurstTime > 0 ?
                    (float)(task.BurstTime - task.RemainingTime) / task.BurstTime * 100 : 0;
                int progressPercent = (int)progress;

                var percentLabel = new Label
                {
                    Text = $"{progressPercent}%",
                    Location = new Point(320, y),
                    Size = new Size(40, 20),
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = Color.FromArgb(0, 120, 215),
                    TextAlign = ContentAlignment.MiddleRight
                };

                // Прогресс-бар
                var progressBar = new Panel
                {
                    Location = new Point(120, y + 3),
                    Size = new Size(barWidth - 140, 14),
                    BackColor = Color.FromArgb(224, 224, 224),
                    BorderStyle = BorderStyle.FixedSingle
                };

                int fillWidth = (int)((barWidth - 142) * progress / 100);
                var progressFill = new Panel
                {
                    Location = new Point(1, 1),
                    Size = new Size(Math.Max(fillWidth, 2), 12),
                    BackColor = task.Color
                };

                progressBar.Controls.Add(progressFill);

                progressPanel.Controls.Add(nameLabel);
                progressPanel.Controls.Add(percentLabel);
                progressPanel.Controls.Add(progressBar);

                y += 30;
            }
        }

        private void UpdateStatusDisplay()
        {
            if (allTasks.Count == 0)
            {
                statusLabel.Text = "Добавьте задачи";
                statusLabel.ForeColor = Color.Gray;
            }
            else if (currentTask != null)
            {
                statusLabel.Text = $"Выполняется: {currentTask.Name}";
                statusLabel.ForeColor = Color.Green;
            }
            else if (readyQueue.Count > 0)
            {
                statusLabel.Text = "Готов к выполнению";
                statusLabel.ForeColor = Color.Blue;
            }
            else if (allTasks.All(t => t.State == TaskState.Completed))
            {
                statusLabel.Text = "Все задачи завершены";
                statusLabel.ForeColor = Color.Green;
            }
            else
            {
                statusLabel.Text = "Готов к работе";
                statusLabel.ForeColor = Color.Green;
            }
        }

        private void UpdateStatus(string message)
        {
            statusLabel.Text = message;
            statusLabel.ForeColor = Color.DarkOrange;
        }

        private void ExecuteStep()
        {
            if (allTasks.Count == 0) return;

            // Если нет текущей задачи, взять из очереди
            if (currentTask == null && readyQueue.Count > 0)
            {
                currentTask = readyQueue.Dequeue();
                currentTask.State = TaskState.Running;
                currentTask.QuantumUsed = 0;
            }

            // Выполнение текущей задачи
            if (currentTask != null)
            {
                currentTask.RemainingTime--;
                currentTask.QuantumUsed++;

                // Проверка завершения
                if (currentTask.RemainingTime <= 0)
                {
                    currentTask.State = TaskState.Completed;
                    currentTask.CompletionTime = DateTime.Now;

                    // Добавляем в отчет
                    completedTasksReport.Add($"{currentTask.Name} : {currentTime}");

                    currentTask = null;
                }
                // Проверка исчерпания кванта
                else if (currentTask.QuantumUsed >= timeQuantum)
                {
                    currentTask.State = TaskState.Ready;
                    readyQueue.Enqueue(currentTask);
                    currentTask = null;
                }
            }

            currentTime++;
            UpdateDisplay();

            // Проверка завершения всех задач
            if (allTasks.All(t => t.State == TaskState.Completed))
            {
                timer.Stop();
                autoButton.Text = "Автоматический";
                UpdateStatus("Все задачи завершены");
            }
        }

        private void AutoButton_Click(object sender, EventArgs e)
        {
            if (allTasks.Count == 0)
            {
                MessageBox.Show("Сначала добавьте задачи!", "Нет задач",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (timer.Enabled)
            {
                timer.Stop();
                autoButton.Text = "Автоматический";
                UpdateStatus("Автоматический режим остановлен");
            }
            else
            {
                timer.Start();
                autoButton.Text = "Остановить";
                UpdateStatus("Автоматический режим запущен");
            }
        }

        private void ResetButton_Click(object sender, EventArgs e)
        {
            timer.Stop();

            // Сброс всех задач
            foreach (var task in allTasks)
            {
                task.RemainingTime = task.BurstTime;
                task.QuantumUsed = 0;
                task.State = TaskState.Ready;
            }

            readyQueue.Clear();
            currentTask = null;
            currentTime = 0;
            completedTasksReport.Clear();

            // Пересоздание очереди
            foreach (var task in allTasks)
            {
                readyQueue.Enqueue(task);
            }

            UpdateDisplay();
            autoButton.Text = "Автоматический";
            UpdateStatus("Симуляция сброшена");
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            ExecuteStep();
        }
    }

    public static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}