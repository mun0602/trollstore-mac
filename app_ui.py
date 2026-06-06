import os
import sys
import threading
import subprocess
from PIL import Image
import customtkinter as ctk

# Cấu hình giao diện CustomTkinter
ctk.set_appearance_mode("Dark")
ctk.set_default_color_theme("green") # Đổi sang green cho hợp với màu neon của TrollStore

def resource_path(relative_path):
    """ Lấy đường dẫn tuyệt đối đến tài nguyên, hoạt động cả khi dev và khi đóng gói bằng PyInstaller """
    try:
        base_path = sys._MEIPASS
    except Exception:
        base_path = os.path.abspath(".")
    return os.path.join(base_path, relative_path)

class App(ctk.CTk):
    def __init__(self):
        super().__init__()

        self.title("TrollStore Installer")
        self.geometry("620x520")
        self.resizable(False, False)

        # Cấu hình grid
        self.grid_rowconfigure(0, weight=1)
        self.grid_columnconfigure(0, weight=1)

        # Khung chứa chính (Main Container)
        self.main_frame = ctk.CTkFrame(self, corner_radius=15)
        self.main_frame.grid(row=0, column=0, padx=20, pady=20, sticky="nsew")
        self.main_frame.grid_columnconfigure(0, weight=1)

        # 1. Phần Header (Logo + Title)
        self.header_frame = ctk.CTkFrame(self.main_frame, fg_color="transparent")
        self.header_frame.grid(row=0, column=0, pady=(20, 10), padx=20, sticky="ew")
        self.header_frame.grid_columnconfigure(1, weight=1)

        # Load logo
        try:
            logo_path = resource_path("icon.png")
            logo_img = Image.open(logo_path)
            self.logo_ctk = ctk.CTkImage(light_image=logo_img, dark_image=logo_img, size=(70, 70))
            self.logo_label = ctk.CTkLabel(self.header_frame, image=self.logo_ctk, text="")
            self.logo_label.grid(row=0, column=0, padx=(10, 20), rowspan=2)
        except Exception as e:
            print(f"Error loading logo: {e}")

        self.label_title = ctk.CTkLabel(
            self.header_frame, 
            text="TrollStore Installer", 
            font=ctk.CTkFont(family="SF Pro Display", size=26, weight="bold"),
            anchor="w"
        )
        self.label_title.grid(row=0, column=1, sticky="w")

        self.label_subtitle = ctk.CTkLabel(
            self.header_frame, 
            text="Công cụ tự động cài đặt TrollStore qua SparseRestore", 
            font=ctk.CTkFont(family="SF Pro Text", size=13, weight="normal"),
            text_color="#888888",
            anchor="w"
        )
        self.label_subtitle.grid(row=1, column=1, sticky="w")

        # Phân cách
        self.separator = ctk.CTkFrame(self.main_frame, height=2, fg_color="#2b2b2b")
        self.separator.grid(row=1, column=0, padx=20, pady=5, sticky="ew")

        # 2. Hướng dẫn & Cài đặt
        self.config_frame = ctk.CTkFrame(self.main_frame, fg_color="transparent")
        self.config_frame.grid(row=2, column=0, padx=20, pady=10, sticky="ew")
        self.config_frame.grid_columnconfigure(1, weight=1)

        # Hướng dẫn nhanh dạng Bullet
        self.label_guide = ctk.CTkLabel(
            self.config_frame,
            text="👉 1. Kết nối iPhone/iPad qua cáp USB và chọn 'Tin cậy' (Trust).\n"
                 "👉 2. Tắt Tìm iPhone (Find My iPhone) trong Cài đặt iCloud.\n"
                 "👉 3. Chọn ứng dụng hệ thống để thay thế (Khuyên dùng: Tips/Mẹo).",
            font=ctk.CTkFont(family="SF Pro Text", size=12),
            justify="left",
            anchor="w"
        )
        self.label_guide.grid(row=0, column=0, columnspan=2, pady=(0, 15), sticky="w")

        # Nhập tên App
        self.label_app = ctk.CTkLabel(self.config_frame, text="Ứng dụng thay thế:", font=ctk.CTkFont(weight="bold"))
        self.label_app.grid(row=1, column=0, padx=(0, 10), sticky="w")

        self.entry_app = ctk.CTkEntry(
            self.config_frame, 
            width=220, 
            placeholder_text="Tips.app",
            font=ctk.CTkFont(family="SF Pro Text", size=13)
        )
        self.entry_app.insert(0, "Tips")
        self.entry_app.grid(row=1, column=1, sticky="w")

        # 3. Thanh tiến trình (Progress Bar)
        self.progress_bar = ctk.CTkProgressBar(self.main_frame, height=8, corner_radius=4)
        self.progress_bar.grid(row=3, column=0, padx=20, pady=10, sticky="ew")
        self.progress_bar.set(0)

        # 4. Bảng hiển thị Log (Console-like)
        self.textbox_log = ctk.CTkTextbox(
            self.main_frame, 
            height=140, 
            font=ctk.CTkFont(family="Courier", size=12),
            fg_color="#181818",
            border_color="#2b2b2b",
            border_width=1,
            state="disabled"
        )
        self.textbox_log.grid(row=4, column=0, padx=20, pady=(5, 10), sticky="nsew")

        # 5. Khung chân trang (Action Button)
        self.action_frame = ctk.CTkFrame(self.main_frame, fg_color="transparent")
        self.action_frame.grid(row=5, column=0, padx=20, pady=(5, 15), sticky="ew")
        self.action_frame.grid_columnconfigure(0, weight=1)

        self.btn_install = ctk.CTkButton(
            self.action_frame, 
            text="Bắt đầu cài đặt (Install)", 
            command=self.start_install_thread, 
            height=44, 
            font=ctk.CTkFont(family="SF Pro Display", size=15, weight="bold")
        )
        self.btn_install.grid(row=0, column=0, sticky="ew")

    def log(self, message):
        self.textbox_log.configure(state="normal")
        self.textbox_log.insert("end", message + "\n")
        self.textbox_log.see("end")
        self.textbox_log.configure(state="disabled")

    def start_install_thread(self):
        app_name = self.entry_app.get().strip()
        if not app_name:
            self.log("[!] Vui lòng nhập tên ứng dụng hệ thống cần thay thế!")
            return

        self.btn_install.configure(state="disabled")
        self.entry_app.configure(state="disabled")
        
        self.textbox_log.configure(state="normal")
        self.textbox_log.delete("1.0", "end")
        self.textbox_log.configure(state="disabled")
        
        self.log("[*] Khởi động tiến trình cài đặt...")
        self.progress_bar.configure(mode="indefinite")
        self.progress_bar.start()

        threading.Thread(target=self.run_install, args=(app_name,), daemon=True).start()

    def run_install(self, app_name):
        try:
            python_exec = sys.executable
            
            # Khởi chạy script trollstore.py ở chế độ nền
            process = subprocess.Popen(
                [python_exec, "trollstore.py"],
                stdin=subprocess.PIPE,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                text=True,
                bufsize=1
            )

            # Truyền tên ứng dụng thay thế vào CLI
            process.stdin.write(app_name + "\n")
            process.stdin.flush()

            # Đọc log đầu ra thời gian thực
            for line in iter(process.stdout.readline, ''):
                self.log(line.strip())

            process.wait()
            
            self.progress_bar.stop()
            self.progress_bar.configure(mode="determinate")
            
            if process.returncode == 0:
                self.progress_bar.set(1.0)
                self.log("\n[+] CÀI ĐẶT THÀNH CÔNG!")
                self.log("[+] Thiết bị của bạn đang tự động khởi động lại...")
                self.log("[+] Vui lòng kích hoạt lại Tìm iPhone sau khi khởi động xong.")
            else:
                self.progress_bar.set(0)
                self.log(f"\n[-] Tiến trình thất bại với mã lỗi: {process.returncode}")

        except Exception as e:
            self.progress_bar.stop()
            self.progress_bar.configure(mode="determinate")
            self.progress_bar.set(0)
            self.log(f"\n[-] Đã xảy ra lỗi hệ thống: {str(e)}")
        
        finally:
            self.btn_install.configure(state="normal")
            self.entry_app.configure(state="normal")

if __name__ == "__main__":
    app = App()
    app.mainloop()
