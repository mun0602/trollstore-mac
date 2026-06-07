using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace TrollRestoreWin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _isInstalling = false;
        private Process? _installProcess = null;

        public MainWindow()
        {
            InitializeComponent();
            txtPassword.Focus();
        }

        // Kéo thả di chuyển cửa sổ
        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        // Nút đóng ứng dụng
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_installProcess != null && !_installProcess.HasExited)
            {
                try
                {
                    _installProcess.Kill();
                }
                catch { }
            }
            this.Close();
        }

        // Sự kiện phím Enter ở ô mật khẩu
        private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CheckPassword();
            }
        }

        // Nút Đăng nhập click
        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            CheckPassword();
        }

        // Kiểm tra mật khẩu xác thực
        private void CheckPassword()
        {
            if (txtPassword.Password == "1305")
            {
                lblError.Visibility = Visibility.Collapsed;
                LoginPanel.Visibility = Visibility.Collapsed;
                InstallerPanel.Visibility = Visibility.Visible;
                txtAppName.Focus();
            }
            else
            {
                lblError.Visibility = Visibility.Visible;
                txtPassword.Password = "";
                txtPassword.Focus();
            }
        }

        // Sao chép Log vào Clipboard
        private void BtnCopyLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(txtLogs.Text);
                MessageBox.Show("Đã sao chép nội dung Log vào Clipboard!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể sao chép Log: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Sửa lỗi Python (Chẩn đoán & Cài đặt thư viện phụ thuộc)
        private void BtnFixPython_Click(object sender, RoutedEventArgs e)
        {
            btnFixPython.IsEnabled = false;
            txtLogs.Text = "[*] Bắt đầu chẩn đoán lỗi Python trên Windows...\n";

            Task.Run(() =>
            {
                string? pythonCmd = GetPythonExecutable();
                if (pythonCmd == null)
                {
                    AppendLog("[-] Không tìm thấy Python được cài đặt trên hệ thống của bạn hoặc chưa được thêm vào PATH.\n");
                    AppendLog("[*] Đang mở trình duyệt tải về Python chính thức cho Windows...\n");
                    AppendLog("[!] LƯU Ý: Khi cài đặt Python, vui lòng tích chọn \"Add Python to PATH\" trước khi nhấn Install.\n");

                    try
                    {
                        Process.Start(new ProcessStartInfo("https://www.python.org/downloads/windows/") { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        AppendLog($"[-] Không thể mở trình duyệt: {ex.Message}\n");
                    }

                    Dispatcher.BeginInvoke(new Action(() => { btnFixPython.IsEnabled = true; }));
                    return;
                }

                AppendLog($"[+] Phát hiện trình biên dịch Python khả dụng: '{pythonCmd}'\n");
                AppendLog("[*] Đang định vị tệp cấu hình phụ thuộc requirements.txt...\n");

                string reqPath = FindFilePath("requirements.txt");
                if (!File.Exists(reqPath))
                {
                    AppendLog("[-] Không tìm thấy file requirements.txt. Đang cài đặt thủ công các thư viện thiết yếu...\n");
                    InstallPythonModules(pythonCmd, "pymobiledevice3 bpylist2 rich requests click Pillow packaging");
                }
                else
                {
                    AppendLog($"[+] Tìm thấy requirements.txt tại: {reqPath}\n");
                    AppendLog("[*] Đang tiến hành cài đặt các thư viện qua pip (Quá trình này có thể mất vài phút)...\n");
                    InstallPythonModules(pythonCmd, $"-r \"{reqPath}\"");
                }

                Dispatcher.BeginInvoke(new Action(() => { btnFixPython.IsEnabled = true; }));
            });
        }

        // Cài đặt các thư viện Python
        private void InstallPythonModules(string pythonCmd, string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = pythonCmd,
                    Arguments = $"-m pip install {arguments}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var proc = Process.Start(psi))
                {
                    if (proc != null)
                    {
                        proc.OutputDataReceived += (s, e) => { if (e.Data != null) AppendLog($"[Pip Output] {e.Data}\n"); };
                        proc.ErrorDataReceived += (s, e) => { if (e.Data != null) AppendLog($"[Pip Error] {e.Data}\n"); };
                        
                        proc.BeginOutputReadLine();
                        proc.BeginErrorReadLine();
                        
                        proc.WaitForExit();

                        if (proc.ExitCode == 0)
                        {
                            AppendLog("[+] Đã cấu hình và cài đặt thành công tất cả thư viện Python cần thiết!\n");
                        }
                        else
                        {
                            AppendLog($"[-] Quá trình cài đặt pip kết thúc với mã lỗi: {proc.ExitCode}\n");
                            AppendLog("[!] Lời khuyên: Hãy thử mở Command Prompt với tư cách Administrator và chạy lệnh:\n");
                            AppendLog($"    {pythonCmd} -m pip install -U pip\n");
                            AppendLog($"    {pythonCmd} -m pip install pymobiledevice3 bpylist2 rich requests click Pillow packaging\n");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[-] Lỗi trong quá trình cài đặt thư viện: {ex.Message}\n");
            }
        }

        // Bắt đầu cài đặt TrollStore
        private void BtnInstall_Click(object sender, RoutedEventArgs e)
        {
            if (_isInstalling) return;

            string appName = txtAppName.Text.Trim();
            if (string.IsNullOrEmpty(appName))
            {
                MessageBox.Show("Vui lòng nhập tên ứng dụng hệ thống sẽ bị thay thế (ví dụ: Tips).", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _isInstalling = true;
            btnInstall.IsEnabled = false;
            txtAppName.IsEnabled = false;
            panelProgress.Visibility = Visibility.Visible;
            txtLogs.Text = "[*] Khởi động tiến trình cài đặt TrollStore...\n";

            Task.Run(() =>
            {
                string? pythonCmd = GetPythonExecutable();
                if (pythonCmd == null)
                {
                    AppendLog("[-] Thất bại: Không tìm thấy Python trên máy tính của bạn.\n");
                    AppendLog("[!] Vui lòng nhấn nút 'Sửa lỗi Python' màu đỏ bên dưới để cài đặt.\n");
                    ResetInstallerUI();
                    return;
                }

                string scriptPath = FindFilePath("trollstore.py");
                if (!File.Exists(scriptPath))
                {
                    AppendLog($"[-] Thất bại: Không tìm thấy file script cài đặt 'trollstore.py' tại đường dẫn làm việc.\n");
                    ResetInstallerUI();
                    return;
                }

                AppendLog($"[+] Sử dụng Python: '{pythonCmd}'\n");
                AppendLog($"[+] Thực thi script: '{scriptPath}'\n");
                AppendLog($"[*] Đang kết nối và chuẩn bị ghi đè ứng dụng hệ thống: {appName}...\n");

                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = pythonCmd,
                        Arguments = $"\"{scriptPath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        RedirectStandardInput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = Path.GetDirectoryName(scriptPath) ?? AppDomain.CurrentDomain.BaseDirectory
                    };

                    _installProcess = new Process { StartInfo = psi };
                    _installProcess.EnableRaisingEvents = true;

                    _installProcess.OutputDataReceived += (s, ev) =>
                    {
                        if (ev.Data != null)
                        {
                            AppendLog(ev.Data + "\n");
                        }
                    };

                    _installProcess.ErrorDataReceived += (s, ev) =>
                    {
                        if (ev.Data != null)
                        {
                            AppendLog("[Error] " + ev.Data + "\n");
                        }
                    };

                    _installProcess.Exited += (s, ev) =>
                    {
                        int exitCode = _installProcess.ExitCode;
                        _installProcess.Dispose();
                        _installProcess = null;

                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            ResetInstallerUI();
                            if (exitCode == 0)
                            {
                                txtLogs.Text += "\n[+] CÀI ĐẶT THÀNH CÔNG!\n[+] Thiết bị của bạn đang tự động khởi động lại. Vui lòng kiểm tra lại sau khi máy lên.";
                            }
                            else
                            {
                                txtLogs.Text += $"\n[-] Tiến trình cài đặt kết thúc không thành công với mã lỗi: {exitCode}\n";
                            }
                            scrollLogs.ScrollToEnd();
                        }));
                    };

                    _installProcess.Start();
                    _installProcess.BeginOutputReadLine();
                    _installProcess.BeginErrorReadLine();

                    // Gửi tên ứng dụng thay thế vào standard input của trollstore.py ngay lập tức
                    _installProcess.StandardInput.WriteLine(appName);

                }
                catch (Exception ex)
                {
                    AppendLog($"[-] Lỗi nghiêm trọng khi khởi động script: {ex.Message}\n");
                    ResetInstallerUI();
                }
            });
        }

        // Đưa UI về trạng thái ban đầu sau khi hoàn tất hoặc lỗi
        private void ResetInstallerUI()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _isInstalling = false;
                btnInstall.IsEnabled = true;
                txtAppName.IsEnabled = true;
                panelProgress.Visibility = Visibility.Collapsed;
            }));
        }

        // Ghi thêm text vào log box một cách an toàn giữa các luồng
        private void AppendLog(string message)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                txtLogs.Text += message;
                scrollLogs.ScrollToEnd();
            }));
        }

        // Tìm tệp tin linh hoạt từ BaseDirectory đến các cấp thư mục cha (cho debug)
        private string FindFilePath(string fileName)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string path = Path.Combine(baseDir, fileName);
            if (File.Exists(path)) return path;

            string currentDir = baseDir;
            for (int i = 0; i < 4; i++)
            {
                var parent = Directory.GetParent(currentDir);
                if (parent == null) break;
                currentDir = parent.FullName;

                path = Path.Combine(currentDir, fileName);
                if (File.Exists(path)) return path;

                path = Path.Combine(currentDir, "TrollRestoreWin", fileName);
                if (File.Exists(path)) return path;
            }

            return Path.Combine(baseDir, fileName); // fallback mặc định
        }

        // Kiểm tra trình thực thi Python nào khả dụng
        private string? GetPythonExecutable()
        {
            string[] commands = { "python", "py", "python3" };
            foreach (var cmd in commands)
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = cmd,
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using (var proc = Process.Start(psi))
                    {
                        if (proc != null)
                        {
                            proc.WaitForExit(2000);
                            if (proc.ExitCode == 0)
                            {
                                return cmd;
                            }
                        }
                    }
                }
                catch
                {
                    // Lệnh không tồn tại, bỏ qua tìm lệnh tiếp theo
                }
            }
            return null;
        }
    }
}
