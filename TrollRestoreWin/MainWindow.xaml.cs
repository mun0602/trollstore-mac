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

        // Chẩn đoán kết nối và Sửa lỗi Driver Apple (thay thế chức năng cài đặt Python vì file exe chạy độc lập)
        private void BtnFixPython_Click(object sender, RoutedEventArgs e)
        {
            btnFixPython.IsEnabled = false;
            txtLogs.Text = "[*] Khởi động chẩn đoán kết nối thiết bị và Driver trên Windows...\n";

            Task.Run(() =>
            {
                AppendLog("[*] Để tương tác với thiết bị iOS qua cổng USB, Windows cần có driver chính thức từ Apple (thường đi kèm iTunes).\n");
                AppendLog("[!] LƯU Ý: Vui lòng sử dụng bản iTunes tải trực tiếp từ trang web của Apple, KHÔNG sử dụng bản tải từ Microsoft Store vì bản đó bị hạn chế quyền truy cập thiết bị.\n\n");
                AppendLog("[*] Đang tự động mở liên kết tải xuống iTunes 64-bit trực tiếp từ Apple...\n");

                try
                {
                    Process.Start(new ProcessStartInfo("https://www.apple.com/itunes/download/win64") { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    AppendLog($"[-] Không thể mở trình duyệt: {ex.Message}\n");
                }

                AppendLog("\n[*] Nếu thiết bị đã kết nối nhưng vẫn báo lỗi USB hoặc DeviceNotConnected:\n");
                AppendLog("[*] Vui lòng cài đặt thêm UsbDk để hỗ trợ USB Passthrough:\n");
                AppendLog("[*] Đang mở liên kết tải xuống UsbDk Releases...\n");

                try
                {
                    Process.Start(new ProcessStartInfo("https://github.com/daynix/UsbDk/releases") { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    AppendLog($"[-] Không thể mở trình duyệt UsbDk: {ex.Message}\n");
                }

                AppendLog("\n[+] Vui lòng cài đặt các driver trên, khởi động lại máy tính và thử lại.\n");

                Dispatcher.BeginInvoke(new Action(() => { btnFixPython.IsEnabled = true; }));
            });
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
                string? exePath = FindInstallExecutable();
                if (exePath == null)
                {
                    AppendLog("[-] Thất bại: Không tìm thấy file thực thi phụ trợ 'TrollRestore.exe' bên cạnh ứng dụng.\n");
                    AppendLog("[!] Đảm bảo bạn đã giải nén đầy đủ tất cả các tệp tin và chạy TrollRestoreWin.exe trong cùng thư mục với TrollRestore.exe.\n");
                    ResetInstallerUI();
                    return;
                }

                AppendLog($"[+] Tìm thấy file thực thi cài đặt: '{exePath}'\n");
                AppendLog($"[*] Đang kết nối và chuẩn bị ghi đè ứng dụng hệ thống: {appName}...\n");

                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = exePath,
                        Arguments = "",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        RedirectStandardInput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = Path.GetDirectoryName(exePath) ?? AppDomain.CurrentDomain.BaseDirectory
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

                    // Gửi tên ứng dụng thay thế vào standard input của TrollRestore.exe ngay lập tức
                    _installProcess.StandardInput.WriteLine(appName);
                }
                catch (Exception ex)
                {
                    AppendLog($"[-] Lỗi nghiêm trọng khi khởi động tiến trình cài đặt: {ex.Message}\n");
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

        // Tìm file thực thi TrollRestore.exe linh hoạt từ BaseDirectory đến các cấp thư mục cha (cho debug)
        private string? FindInstallExecutable()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string path = Path.Combine(baseDir, "TrollRestore.exe");
            if (File.Exists(path)) return path;

            string currentDir = baseDir;
            for (int i = 0; i < 4; i++)
            {
                var parent = Directory.GetParent(currentDir);
                if (parent == null) break;
                currentDir = parent.FullName;

                path = Path.Combine(currentDir, "TrollRestore.exe");
                if (File.Exists(path)) return path;

                path = Path.Combine(currentDir, "dist", "TrollRestore.exe");
                if (File.Exists(path)) return path;

                path = Path.Combine(currentDir, "TrollRestoreWin", "TrollRestore.exe");
                if (File.Exists(path)) return path;
            }

            return null;
        }
    }
}
