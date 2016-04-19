using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows;


namespace Client
{
    public partial class Form1 : Form
    {

        static private Socket Client;
        private IPAddress ip = null;
        private int port = 0;
        private Thread th;

        public Form1()
        {
            InitializeComponent();

            richTextBox1.Enabled = false;
            richTextBox2.Enabled = false;
            button1.Enabled = false;
            //button3.Enabled = false;

            //Создаем функцию записи/чтения в файл с обработчиком ошибок
            try
            {
                var sr = new StreamReader(@"Client_info/data_info.txt");
                string buffer = sr.ReadToEnd();                
                sr.Close();
                string[] connect_info = buffer.Split(':');
                if (connect_info[0] == "localhost") { connect_info[0]="127.0.0.1";}
                else if (connect_info[0] == "dom.ru") { connect_info[0] = "109.194.72.178"; }
                else
                {
                    ip = IPAddress.Parse(connect_info[0]);
                    port = int.Parse(connect_info[1]);
                }

                label4.ForeColor = Color.Blue;
                label4.Text = "IP:    " + connect_info[0] + "\nPort: " + connect_info[1];
            }
            catch (Exception ex) 
                {
                    label4.ForeColor = Color.Red;
                    label4.Text = "Не настроено";
                    Form2 form = new Form2();
                    form.Show();
                }



        }
        //Делегат доступа к контролам формы
        delegate void SendMsg(String Text, RichTextBox Rtb);

        SendMsg AcceptDelegate = (String Text, RichTextBox Rtb) =>
        {
            Rtb.Text += Text + "\n";
        };
        protected void FileReceiver()
        {
            //Создаем Listener на порт "по умолчанию"
            TcpListener Listen = new TcpListener(6999);
            //Начинаем прослушку
            Listen.Start();
            //и заведем заранее сокет
            Socket ReceiveSocket;
            while (true)
            {
                try
                {
                    //Пришло сообщение
                    ReceiveSocket = Listen.AcceptSocket();
                    Byte[] buffer = new Byte[256];
                    //Читать сообщение будем в поток
                    using (MemoryStream MessageR = new MemoryStream())
                    {

                        //Количество считанных байт
                        Int32 ReceivedBytes;
                        Int32 Firest256Bytes = 0;
                        String FilePath = "";
                        do
                        {//Собственно читаем
                            ReceivedBytes = ReceiveSocket.Receive(buffer, buffer.Length, 0);
                            //Разбираем первые 256 байт
                            if (Firest256Bytes < 256)
                            {
                                Firest256Bytes += ReceivedBytes;
                                Byte[] ToStr = buffer;
                                //Учтем, что может возникнуть ситуация, когда они не могу передаться "сразу" все
                                if (Firest256Bytes > 256)
                                {
                                    Int32 Start = Firest256Bytes - ReceivedBytes;
                                    Int32 CountToGet = 256 - Start;
                                    Firest256Bytes = 256;
                                    //В случае если было принято >256 байт (двумя сообщениями к примеру)
                                    //Остаток (до 256) записываем в "путь файла"
                                    ToStr = buffer.Take(CountToGet).ToArray();
                                    //А остальную часть - в будующий файл
                                    buffer = buffer.Skip(CountToGet).ToArray();
                                    MessageR.Write(buffer, 0, ReceivedBytes);
                                }
                                //Накапливаем имя файла
                                FilePath += Encoding.Default.GetString(ToStr);
                            }
                            else

                                //и записываем в поток
                                MessageR.Write(buffer, 0, ReceivedBytes);
                            //Читаем до тех пор, пока в очереди не останется данных
                        } while (ReceivedBytes == buffer.Length);
                        //Убираем лишние байты
                        String resFilePath = FilePath.Substring(0, FilePath.IndexOf('\0'));
                        using (var File = new FileStream(resFilePath, FileMode.Create))
                        {//Записываем в файл
                            File.Write(MessageR.ToArray(), 0, MessageR.ToArray().Length);
                        }//Уведомим пользователя
                        richTextBox1.BeginInvoke(AcceptDelegate, new object[] { "Received: " + resFilePath, richTextBox1 });
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

            }
        }

        private void настройкиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form2 form = new Form2();
            form.Show();
        }
        void SendMessage(string message)
        {
            if (message !=" " && message !="")
            {
                byte[] buffer = new byte[1024];
                buffer = Encoding.UTF8.GetBytes(message);
                Client.Send(buffer);
            }
        }
        void RecvMessage()
        {
            byte[] buffer = new byte[1024];
                for (int i=0; i<buffer.Length; i++)
                {
                    buffer[i]=0;
                }
                
                for(; ; )
                {
                    try
                    {
                        Client.Receive(buffer);
                        string message = Encoding.UTF8.GetString(buffer);
                        int count = message.IndexOf("$end$");
                        if (count == -1)
                        {
                            continue;
                        }
                        string Clear_Message = "";

                        for (int i=0; i<count; i++)
                        {
                            Clear_Message += message[i];
                        }
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            buffer[i] = 0;
                        }
                        this.Invoke((MethodInvoker)delegate()
                        {
                            richTextBox1.AppendText(Clear_Message);
                        });
                    }
                    catch (Exception ex) { }
                }

        }

        private void button2_Click(object sender, EventArgs e)
        {
               if(textBox1.Text!=" " && textBox1.Text!="")
               {
                   button1.Enabled = true;
                   richTextBox2.Enabled = true;
                   Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                   if (ip != null) {  
                       Client.Connect(ip, port);  
                       th=new Thread(delegate(){ RecvMessage(); });
                       th.Start(); // Запускаеся поток принимающий сообщения с сервера
                   }
               }
               
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SendMessage("\n" + textBox1.Text + ": " + richTextBox2.Text + "$end$");
            richTextBox2.Clear();
        }


        private void выходToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Перед выходом поток останавливаем
            if (th != null) th.Abort();
            if (Client != null)
            {
                Client.Close();
            }
            Application.Exit();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void авторToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private void авторToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Create By -=Specter=- ADT-Tean.com (2016)");
        }

        private void оПрограммеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Данный чат создан в качестве пособия для изучения С++");
        }       
    }
}
