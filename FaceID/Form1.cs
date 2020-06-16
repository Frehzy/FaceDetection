using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Management;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using MetroFramework.Forms;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Data;
using Microsoft.VisualBasic.FileIO;

namespace FaceID
{
    public partial class Form1 : MetroForm
    {
        //Объявление всех переменных, используемых в программе
        MCvFont font = new MCvFont(Emgu.CV.CvEnum.FONT.CV_FONT_HERSHEY_TRIPLEX, 0.6d, 0.6d);
        HaarCascade faceDetected;
        Image<Bgr, Byte> Frame;
        Emgu.CV.Capture camera;
        Image<Gray, byte> result;
        Image<Gray, byte> TrainedFace = null;
        Image<Gray, byte> grayFace = null;
        List<Image<Gray, byte>> trainingImames = new List<Image<Gray, byte>>();
        List<string> labels = new List<string>();
        List<string> Users = new List<string>();
        int Count, NumLables, t;
        string name, names = null;
        int i;
        bool isConnected = false;
        FilterInfoCollection filterInfoCollection;
        VideoCaptureDevice videoCaptureDevice;
        int Width, Height;
        int number = 1;
        int index_folder = 1;

        //Проверка, пустая ли папка с базой (баг)
        public Form1()
        {
            //проверка на всякий случай
            try
            {
                InitializeComponent();

                //если папка с базой не существует
                String dir = Application.StartupPath + "/Faces";
                //если не существует, создаём папку
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                //если папка с отчётом не существует
                String report = Application.StartupPath + "/report";
                //если не существует, создаём папку
                if (!Directory.Exists(report))
                {
                    Directory.CreateDirectory(report);
                }

                //создание ТХТ файла с report
                File.Delete(Application.StartupPath + "/report.csv");
                File.WriteAllText(Application.StartupPath + "/report.csv", "№,Name,Date and Time \n");
                faceDetected = new HaarCascade("haarcascade_frontalface_default.xml");
            }
            catch (Exception)
            {
                //вывод сообщения, что всё плохо
                MessageBox.Show("Что-то пошло не так. Попробуйте перезапустить приложение", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        //кнопка "Выход"
        private void metroButton3_Click(object sender, EventArgs e)
        {
            var report = MessageBox.Show("Удалить отчёт?", "Удалить отчёт?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            DirectoryInfo reportfolders = new DirectoryInfo(Application.StartupPath + "/report/");
            if (report == DialogResult.Yes)
            {
                File.Delete(Application.StartupPath + "/report.csv");
                try
                {
                    foreach (FileInfo file in reportfolders.EnumerateFiles())
                    {
                        try
                        {
                            file.Delete();
                        }
                        catch (Exception) { }
                    }
                    foreach (DirectoryInfo dir in reportfolders.EnumerateDirectories())
                    {
                        try
                        {
                            dir.Delete(true);
                        }
                        catch (Exception) { }
                    }
                }
                catch (Exception)
                { }
                try
                {
                    reportfolders.Delete();
                }
                catch { }
            }
            else if (report == DialogResult.No)
            {
                try
                {
                    DateTime time = DateTime.Now;
                    //копирование папки с фото-отчётом
                    if (Directory.Exists(Application.StartupPath + "/report"))
                    {
                        FileSystem.CopyDirectory(Application.StartupPath + "/report", Application.StartupPath + $"/report {time.ToString("D")}_{index_folder}");
                        FileSystem.DeleteDirectory(Application.StartupPath + "/report", DeleteDirectoryOption.DeleteAllContents);
                    }
                    else
                    {
                        MessageBox.Show("Похоже, что папка с фото-отчётом была удалена или не была создана","Невозможно сохранить папку с фото-отчётом",MessageBoxButtons.OK,MessageBoxIcon.Error);
                    }
                    //копирование отчёта в папку с фото-отчётом
                    if(File.Exists(Application.StartupPath + "/report.csv"))
                    {
                        File.Copy(Application.StartupPath + "/report.csv", Application.StartupPath + $"/report {time.ToString("D")}_{index_folder}/report {time.ToString("D")}.txt");
                        File.Delete(Application.StartupPath + $"/report.csv");
                    }
                    else
                    {
                        try
                        {
                            //если папка с отчётом не существует
                            String reportfolderstemp = Application.StartupPath + $"/report {time.ToString("D")}_{index_folder}";
                            //если не существует, создаём папку
                            if (!Directory.Exists(reportfolderstemp))
                            {
                                Directory.CreateDirectory(reportfolderstemp);
                            }
                            File.Copy(Application.StartupPath + "/report.csv", Application.StartupPath + $"/report {time.ToString("D")}_{index_folder}/report {time.ToString("D")}.txt");
                            File.Delete(Application.StartupPath + $"/report.csv");
                        }
                        catch
                        {
                            MessageBox.Show("Похоже, что отчёт был удален или не был создан", "Невозможно сохранить отчёт", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                catch (Exception)
                {
                    DateTime time = DateTime.Now;
                    MessageBox.Show("Ошибка поиска файлов отчёта. Попытка сохранить любые файлы и закрытие программы", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    try
                    {
                        //если папка с отчётом не существует
                        String reportfolderstemp = Application.StartupPath + $"/report {time.ToString("D")}_{index_folder}";
                        //если не существует, создаём папку
                        if (!Directory.Exists(reportfolderstemp))
                        {
                            Directory.CreateDirectory(reportfolderstemp);
                        }
                        File.Copy(Application.StartupPath + "/report.csv", Application.StartupPath + $"/report {time.ToString("D")}_{index_folder}/report {time.ToString("D")}.txt");
                        File.Delete(Application.StartupPath + $"/report.csv");
                    }
                    catch { }
                }
                index_folder++;
            }
            //проверка существования папки
            DirectoryInfo di = new DirectoryInfo(Application.StartupPath + "/Faces/");
            //удаление базы лиц, если она существует
            try
            {
                foreach (FileInfo file in di.EnumerateFiles())
                {
                    try
                    {
                        file.Delete();
                    }
                    catch (Exception) { }
                }
                foreach (DirectoryInfo dir in di.EnumerateDirectories())
                {
                    try
                    {
                        dir.Delete(true);
                    }
                    catch (Exception) { }
                }
                //чистка всех переменных
                di.Delete();
                Count = 0;
                trainingImames.Clear();
                labels.Clear();
                name = null;
                metroComboBox1.Items.Clear();
            }
            //иначе, просто выход из программы
            catch (Exception)
            { }
            //выйти из программы
            Close();
            Environment.Exit(0);
            Application.Exit();
        }

        //Кнопка "Очистить базу лиц"
        private void metroButton4_Click(object sender, EventArgs e)
        {
            //поиск этой директории и удаление папки с базой лиц
            DirectoryInfo di = new DirectoryInfo(Application.StartupPath + "/Faces/");
            //проверка на существование базы лиц
            try
            {
                foreach (FileInfo file in di.EnumerateFiles())
                {
                    try
                    {
                        file.Delete();
                    }
                    catch (Exception) { }
                }
                foreach (DirectoryInfo dir in di.EnumerateDirectories())
                {
                    try
                    {
                        dir.Delete(true);
                    }
                    catch (Exception) { }
                }
                //чистка всех переменных
                di.Delete();
                Count = 0;
                trainingImames.Clear();
                labels.Clear();
                name = null;
                metroComboBox1.Items.Clear();
            }
            catch (Exception)
            {
                //вывод сообщения грусти
                MessageBox.Show("База лиц уже удалена или не существует", "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    
        //Кнопка "Начать работу алгоритма"
        private void start_Click(object sender, EventArgs e)
        {
            //прячу и показываю необходимые кнопки
            start.Visible = false;
            metroButton1.Visible = true;
            label1.Visible = true;
            textName.Visible = true;
            metroComboBox1.Visible = true;
            metroCheckBox1.Visible = true;
            metroLabel1.Visible = true;
            metroLabel2.Visible = true;
            metroTrackBar1.Visible = true;
            metroLabel4.Visible = true;
            metroLabel3.Visible = true;
            metroLabel5.Visible = true;
            metroLabel6.Visible = true;
            metroLabel7.Visible = false;
            metroButton2.Visible = true;
            metroButton5.Visible = true;
            metroButton6.Visible = true;
            //проверяю, существует ли камера и подключаюсь к ней, показываю её и запускаю процедуру
            try
            {
                camera = new Emgu.CV.Capture();
                camera.QueryFrame();
                Application.Idle += new EventHandler(FrameProcedure);
            }
            catch (Exception)
            {
                //вывод сообщения и закрытие приложения, если всё плохо
                var camfail = MessageBox.Show("Похоже, что камера не была обнаружена. Вы уверены, что камера подключена и стабильно работает?", "Камера не обнаружена", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (camfail == DialogResult.Yes)
                {
                    try
                    {
                        camera = new Emgu.CV.Capture();
                        camera.QueryFrame();
                        Application.Idle += new EventHandler(FrameProcedure);
                    }
                    catch { }
                }
                if (camfail == DialogResult.No)
                {
                    MessageBox.Show("Попробуйте перезапустить компьютер. Если ошибка не исчезла, сообщите на почту: frehzzzy@gmail.com","Спасибо",MessageBoxButtons.OK,MessageBoxIcon.None);
                    DirectoryInfo di = new DirectoryInfo(Application.StartupPath + "/Faces/");
                    try
                    {
                        foreach (FileInfo file in di.EnumerateFiles())
                        {
                            try
                            {
                                file.Delete();
                            }
                            catch (Exception) { }
                        }
                        foreach (DirectoryInfo dir in di.EnumerateDirectories())
                        {
                            try
                            {
                                dir.Delete(true);
                            }
                            catch (Exception) { }
                        }
                        //чистка всех переменных
                        di.Delete();
                        Count = 0;
                        trainingImames.Clear();
                        labels.Clear();
                        name = null;
                        metroComboBox1.Items.Clear();
                        Environment.Exit(0);
                        Application.Exit();
                    }
                    catch
                    {
                        //повторная чистка и закрытие приложения
                        Count = 0;
                        trainingImames.Clear();
                        labels.Clear();
                        name = null;
                        metroComboBox1.Items.Clear();
                        Close();
                        Environment.Exit(0);
                        Application.Exit();
                    }
                    Close();
                    Environment.Exit(0);
                    Application.Exit();
                }
            }
        }

        //Если всё плохо, выход из проги и чистка всего
        private void ifCamNotOkey()
        {
            var camfail = MessageBox.Show("Похоже, что камера не была обнаружена. Вы уверены, что камера подключена и стабильно работает?", "Камера не обнаружена", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (camfail == DialogResult.Yes)
            { }
            if (camfail == DialogResult.No)
            {
                MessageBox.Show("Попробуйте перезапустить компьютер. Если ошибка не исчезла, сообщите на почту: frehzzzy@gmail.com", "Спасибо", MessageBoxButtons.OK, MessageBoxIcon.None);
                //удаление папки с лицами
                DirectoryInfo di = new DirectoryInfo(Application.StartupPath + "/Faces/");
                try
                {
                    foreach (FileInfo file in di.EnumerateFiles())
                    {
                        try
                        {
                            file.Delete();
                        }
                        catch (Exception) { }
                    }
                    foreach (DirectoryInfo dir in di.EnumerateDirectories())
                    {
                        try
                        {
                            dir.Delete(true);
                        }
                        catch (Exception) { }
                    }
                    //чистка всех переменных
                    di.Delete();
                    Count = 0;
                    trainingImames.Clear();
                    labels.Clear();
                    name = null;
                    metroComboBox1.Items.Clear();
                    Environment.Exit(0);
                    Application.Exit();
                }
                catch
                {
                    //повторная чистка и закрытие приложения
                    Count = 0;
                    trainingImames.Clear();
                    labels.Clear();
                    name = null;
                    metroComboBox1.Items.Clear();
                    Close();
                    Environment.Exit(0);
                    Application.Exit();
                }
                Close();
                Environment.Exit(0);
                Application.Exit();
            }
        }


        //кнопка "Добавить лицо в базу"
        private void metroButton1_Click(object sender, EventArgs e)
        {
            //на всякий случай проверка на успешность
            try
            {
                //проверка, существует ли папка с базой
                String dir = Application.StartupPath + "/Faces";
                //если не существует, создаю эту папку
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
            catch (Exception)
            {
                //вывод ошибки и закрытие программы
                MessageBox.Show("Что-то пошло не так. Попробуйте перезапустить приложение", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ifCamNotOkey();
            }
            if (textName.Text == "")
            {
                MessageBox.Show("Похоже, что вы не ввели имя для лица", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                //проверка при ошибочном ПЕРВОМ добавлении лица
                try
                {
                    //счётчик, считающий количество "лиц" в базе
                    Count = Count + 1;
                    //устанавливаю разрешение камеры
                    grayFace = camera.QueryGrayFrame().Resize(855, 588, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                    //ищу прямоугольные области, в которых есть лица (размер "квадрата" - 20 на 20)
                    MCvAvgComp[][] DetectedFaces = grayFace.DetectHaarCascade(faceDetected, 1.2, 10, Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(20, 20));
                    foreach (MCvAvgComp f in DetectedFaces[0])
                    {
                        TrainedFace = Frame.Copy(f.rect).Convert<Gray, byte>();
                        break;
                    }
                    //лица, которые я нашёл, "фотографирую", уменьшая размер фото 100 на 100 и делая его ЧБ
                    TrainedFace = result.Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                    //добавляю это лицо в базу
                    trainingImames.Add(TrainedFace);
                    //запоминаю имя этого лица
                    labels.Add(textName.Text);
                    //счётчик лиц + добавление изменённых лиц в папку с базой
                    File.WriteAllText(Application.StartupPath + "/Faces/Faces.txt", "Количество лиц: " + trainingImames.ToArray().Length.ToString() + "\n");
                    //добавление новых лиц в папку
                    for (int i = 1; i < trainingImames.ToArray().Length + 1; i++)
                    {
                        trainingImames.ToArray()[i - 1].Save(Application.StartupPath + "/Faces/face" + i + ".bmp");
                        //запись имени лица в текстовый документ
                        File.AppendAllText(Application.StartupPath + "/Faces/Faces.txt", labels.ToArray()[i - 1] + "\n");

                    }
                    //вывод сообщения о добавлении лица в базу лиц
                    MessageBox.Show(textName.Text + " - лицо успешно добавлено в базу лиц", "Успех!", MessageBoxButtons.OK,MessageBoxIcon.Information);
                    metroComboBox1.Items.Clear();
                    string[] text = File.ReadAllLines(Application.StartupPath + "/Faces/Faces.txt");
                    //чистка окна для ввода лица от прошлого сообщения
                    metroComboBox1.Items.AddRange(text);
                    textName.Text = "";
                }
                catch (Exception)
                {
                    //если лицо не обнаружено
                    MessageBox.Show("При первом добавлении лица возникла ошибка", "Лицо не обнаружено!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        //процедура по сверке лица из базы с камерой
        private void FrameProcedure(object sender, EventArgs e)
        {
            //проверка на всякий случай
            try
            {
                //добавляю новое лицо, не имеющее (пока что) имени
                Users.Add("");
                //создаю объект Frame, устанавливаю для него разрешение камеры (855х588)
                Frame = camera.QueryFrame().Resize(cameraBox.Width, cameraBox.Height, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                //делаю изображение чёрно-белым
                grayFace = Frame.Convert<Gray, Byte>();
                MCvAvgComp[][] facesDetectedNow = grayFace.DetectHaarCascade(faceDetected, 1.2, 10, Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(20, 20));
                //беру КАЖДОЕ фото из "базы лиц" и делаю для каждого лица следующее
                foreach (MCvAvgComp f in facesDetectedNow[0])
                {
                    //изменяю размер "лица", делаю его чёрно-белым
                    result = Frame.Copy(f.rect).Convert<Gray, Byte>().Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                    //делаю обводку лица зелёного цвета в ширине линии = 3
                    Frame.Draw(f.rect, new Bgr(Color.Green), 3);
                    //если фото из базы совпадает, то....
                    if (trainingImames.ToArray().Length != 0)
                    {
                        MCvTermCriteria termCriterias = new MCvTermCriteria(Count, 0.001);
                        //распознаю лицо из базы (создаю объект, который распознаёт лицо)
                        EigenObjectRecognizer recognizer = new EigenObjectRecognizer(trainingImames.ToArray(), labels.ToArray(), 1500, ref termCriterias);
                        //вывожу имя (name)
                        name = recognizer.Recognize(result);
                        //делаю подпись имени лица в зависимости от координат, в которых находится зелёный "квадрат" обводки лица
                        //т.е. отталкиваюсь от предыдущих координат лица, которые получены были выше
                        Frame.Draw(name, ref font, new Point(f.rect.X - 2, f.rect.Y - 2), new Bgr(Color.Red));
                    }
                    //если ничего не нашёл, печатаю пустоту
                    Users.Add("");
                }
                //вывод найденого лица в label4
                if (name == "")
                {
                    metroLabel3.Text = "Лица нет в базе лиц";
                    metroLabel5.Text = "или";
                    metroLabel6.Text = "Лицо не найдено";
                }
                else
                {
                    metroLabel3.Text = name;
                    metroLabel5.Text = "";
                    metroLabel6.Text = "";
                }
                //пытаюсьь, подключена ли камера. В случае чего, вывожу ошибку
                try
                {
                    cameraBox.Image = Frame;
                    name = "";
                    Users.Clear();
                }
                catch (Exception)
                {
                    //вывод сообщения и закрытие приложения, если всё плохо
                    var camfail = MessageBox.Show("Похоже, что камера не была обнаружена. Вы уверены, что камера подключена и стабильно работает?", "Камера не обнаружена", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (camfail == DialogResult.Yes)
                    {
                        cameraBox.Image = Frame;
                        name = "";
                        Users.Clear();
                    }
                    if (camfail == DialogResult.No)
                    {
                        MessageBox.Show("Попробуйте перезапустить компьютер. Если ошибка не исчезла, сообщите на почту: frehzzzy@gmail.com", "Спасибо", MessageBoxButtons.OK, MessageBoxIcon.None);
                        Close();
                        Environment.Exit(0);
                        Application.Exit();
                    }
                }
            }
            catch (Exception)
            {
                //вывод сообщения и закрытие приложения, если всё плохо
                var camfail = MessageBox.Show("Похоже, что камера не была обнаружена. Вы уверены, что камера подключена и стабильно работает?", "Камера не обнаружена", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (camfail == DialogResult.Yes)
                { }
                if (camfail == DialogResult.No)
                {
                    MessageBox.Show("Попробуйте перезапустить компьютер. Если ошибка не исчезла, сообщите на почту: frehzzzy@gmail.com", "Спасибо", MessageBoxButtons.OK, MessageBoxIcon.None);
                    Close();
                    Environment.Exit(0);
                    Application.Exit();
                }
            }
        }

        //поднимает форму поверх всех окон и проверяет камеру
        private void Form1_Load(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
            Show();
            WindowState = FormWindowState.Maximized;
            //установление максимальных размеров для формы
            Width = Size.Width;
            Height = Size.Height;
            this.MinimumSize = new Size(Width,Height);
            this.MaximumSize = new Size(Width, Height);
            //проверка существования камеры
            try
            {
                filterInfoCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                foreach (FilterInfo filterInfo in filterInfoCollection)
                    metroComboBox2.Items.Add(filterInfo.Name);
                metroComboBox2.SelectedIndex = 0;
                videoCaptureDevice = new VideoCaptureDevice();
            }
            catch
            {
                //вывод сообщения и закрытие приложения, если всё плохо
                var camfail = MessageBox.Show("Похоже, что камера не была обнаружена. Вы уверены, что камера подключена и стабильно работает?", "Камера не обнаружена", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (camfail == DialogResult.Yes)
                { }
                if (camfail == DialogResult.No)
                {
                    MessageBox.Show("Попробуйте перезапустить компьютер. Если ошибка не исчезла, сообщите на почту: frehzzzy@gmail.com", "Спасибо", MessageBoxButtons.OK, MessageBoxIcon.None);
                    Close();
                    Environment.Exit(0);
                    Application.Exit();
                }
            }
        }

        //появление надписей при наведении мыши
        private void metroButton1_MouseEnter(object sender, EventArgs e)
        {
            metroLabel8.Visible = true;
            metroLabel9.Visible = true;
        }

        //Открытие окна с отчётом
        private void metroButton2_Click(object sender, EventArgs e)
        {
            Data.IsClickToReport = true;
            Report report = new Report();
            report.ShowDialog();
        }

        private void metroButton5_Click(object sender, EventArgs e)
        {
            DirectoryInfo reportfolders = new DirectoryInfo(Application.StartupPath + "/report/");
            try
            {
                DateTime time = DateTime.Now;
                //копирование папки с фото-отчётом
                if (Directory.Exists(Application.StartupPath + "/report"))
                {
                    FileSystem.CopyDirectory(Application.StartupPath + "/report", Application.StartupPath + $"/report {time.ToString("D")}_{index_folder}");
                    MessageBox.Show("Папка с фото-отчётом успешно сохранена", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Похоже, что папка с фото-отчётом была удалена или не была создана", "Невозможно сохранить папку с фото-отчётом", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //копирование отчёта в папку с фото-отчётом
                if (File.Exists(Application.StartupPath + "/report.csv"))
                {
                    File.Copy(Application.StartupPath + "/report.csv", Application.StartupPath + $"/report {time.ToString("D")}_{index_folder}/report {time.ToString("D")}.txt");
                    MessageBox.Show("Отчёт успешно сохранён", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    try
                    {
                        //если папка с отчётом не существует
                        String reportfolderstemp = Application.StartupPath + $"/report {time.ToString("D")}_{index_folder}";
                        //если не существует, создаём папку
                        if (!Directory.Exists(reportfolderstemp))
                        {
                            Directory.CreateDirectory(reportfolderstemp);
                        }
                        File.Copy(Application.StartupPath + "/report.csv", Application.StartupPath + $"/report {time.ToString("D")}_{index_folder}/report {time.ToString("D")}.txt");
                        MessageBox.Show("Отчёт успешно сохранён", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch
                    {
                        MessageBox.Show("Похоже, что отчёт был удален или не был создан", "Невозможно сохранить отчёт", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception)
            {
                DateTime time = DateTime.Now;
                MessageBox.Show("Ошибка поиска файлов отчёта. Попытка сохранить любые файлы", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                try
                {
                    //если папка с отчётом не существует
                    String reportfolderstemp = Application.StartupPath + $"/report {time.ToString("D")}_{index_folder}";
                    //если не существует, создаём папку
                    if (!Directory.Exists(reportfolderstemp))
                    {
                        Directory.CreateDirectory(reportfolderstemp);
                    }
                    File.Copy(Application.StartupPath + "/report.csv", Application.StartupPath + $"/report {time.ToString("D")}_{index_folder}/report {time.ToString("D")}.txt");
                    MessageBox.Show("Отчёт успешно сохранён", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch { }
            }
            index_folder++;
        }

        private void metroButton6_Click(object sender, EventArgs e)
        {
            try
            {
                File.Delete(Application.StartupPath + "/report.csv");
                //создание ТХТ файла с report
                File.Delete(Application.StartupPath + "/report.csv");
                File.WriteAllText(Application.StartupPath + "/report.csv", "№,Name,Date and Time \n");
            }
            catch { }
            try
            {
                if (Directory.Exists(Application.StartupPath + "/report"))
                {
                    FileSystem.DeleteDirectory(Application.StartupPath + "/report", DeleteDirectoryOption.DeleteAllContents);
                    String reportfolderstemp = Application.StartupPath + $"/report";
                    if (!Directory.Exists(reportfolderstemp))
                    {
                        Directory.CreateDirectory(reportfolderstemp);
                    }
                }
            }
            catch { }
        }

        //исчезновение надписи, если мышь не наведена на надпись
        private void metroButton1_MouseLeave(object sender, EventArgs e)
        {
            metroLabel8.Visible = false;
            metroLabel9.Visible = false;
        }

        //Чек-бокс "Искать...", запуск таймера
        private void metroCheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            //проверка на всякий случай
            try
            {
                //обнуление и запуск таймера
                i = 0;
                timer1.Start();
                timer1.Enabled = true;
            }
            catch (Exception)
            {
                //вывод ошибки и закрытие приложение
                MessageBox.Show("Что-то пошло не так. Попробуйте перезапустить приложение", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ifCamNotOkey();
            }
        }
        
        //поиск лица
        private void timer1_Tick(object sender, EventArgs e)
        {
            //проверка (на всякий случай)
            try
            {
                //если таймер включен, то
                if (timer1.Enabled == true)
                {
                    //исчезает трекбар и начинается счёт секунд
                    metroTrackBar1.Visible = false;
                    if (i <= Convert.ToInt32(metroLabel2.Text))
                    {
                        //вкл и выкл отображение счётчика
                        metroLabel7.Visible = true;
                        metroLabel2.Visible = false;
                        i++;
                        metroLabel7.Text = Convert.ToString(Convert.ToInt32(metroLabel2.Text) - i);
                        //если не выбран элемент в списке
                        if (string.IsNullOrEmpty(metroComboBox1.Text))
                        {
                            //отключение отсчёта, появление необходимых надписей и трекбара и вывод ошибки
                            timer1.Enabled = false;
                            metroCheckBox1.Checked = false;
                            metroTrackBar1.Visible = true;
                            metroLabel7.Visible = false;
                            metroLabel2.Visible = true;
                            timer1.Stop();
                            MessageBox.Show("Не выбран элемент списка", "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                            //если нашёл лицо
                            if (Convert.ToString(metroComboBox1.Text) == Convert.ToString(metroLabel3.Text))
                            {
                                //отключение отсчёта, появление необходимых надписей и трекбара и вывод найденного лица
                                timer1.Enabled = false;
                                metroCheckBox1.Checked = false;
                                metroTrackBar1.Visible = true;
                                metroLabel7.Visible = false;
                                metroLabel2.Visible = true;
                                timer1.Stop();
                                MessageBox.Show("Найдено лицо: " + metroComboBox1.Text, "Успех!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                //запись инфы в ТХТ report
                                StreamWriter report = new StreamWriter(Application.StartupPath + "/report.csv", true);
                                DateTime time = DateTime.Now;
                                report.Write(number + "," + metroComboBox1.Text + "," + time + "\n");
                                report.Close();
                                //если папка с отчётом не существует
                                String reportfolderstemp = Application.StartupPath + $"/report";
                                //если не существует, создаём папку
                                if (!Directory.Exists(reportfolderstemp))
                                {
                                    Directory.CreateDirectory(reportfolderstemp);
                                }
                                cameraBox.Image.Save(Application.StartupPath + $"/report/{time.ToString("D")} {metroComboBox1.Text}_{number}.png");
                                //передача инфы в общую базу переменных (data.cs) - порядковый номер и имя человека
                                Data.number = number;
                                Data.name = metroComboBox1.Text;
                                number++;
                            }
                        }
                        //если счёт дошёл до макс.значения и не нашёл лицо
                        if (i == Convert.ToInt32(metroLabel2.Text))
                        {
                            //отключение отсчёта, появление необходимых надписей и трекбара и вывод ошибки
                            timer1.Enabled = false;
                            metroCheckBox1.Checked = false;
                            metroTrackBar1.Visible = true;
                            metroLabel7.Visible = false;
                            metroLabel2.Visible = true;
                            timer1.Stop();
                            MessageBox.Show("Не найдено лицо", "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception)
            {
                //вывод ошибки и закрытие приложения
                MessageBox.Show("Что-то пошло не так. Попробуйте перезапустить приложение", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ifCamNotOkey();
            }
        }

        //Выбор времени поиска
        private void metroTrackBar1_Scroll(object sender, ScrollEventArgs e)
        {
            metroLabel2.Text = metroTrackBar1.Value.ToString();
        }
    }
}