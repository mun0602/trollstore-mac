import os
import sys
import socket
import time
import http.server
import socketserver
import threading
import subprocess

def get_local_ip():
    s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    try:
        s.connect(('8.8.8.8', 80))
        ip = s.getsockname()[0]
    except Exception:
        ip = '127.0.0.1'
    finally:
        s.close()
    return ip

def start_server(directory, port):
    # Tránh log in nhiều lên màn hình
    class QuietHandler(http.server.SimpleHTTPRequestHandler):
        def log_message(self, format, *args):
            pass
        def __init__(self, *args, **kwargs):
            super().__init__(*args, directory=directory, **kwargs)
            
    socketserver.TCPServer.allow_reuse_address = True
    server = socketserver.TCPServer(("", port), QuietHandler)
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    return server

def show_qr(text):
    try:
        import qrcode
        qr = qrcode.QRCode()
        qr.add_data(text)
        qr.print_ascii(invert=True)
    except ImportError:
        # Nếu chưa cài qrcode thì hiển thị link thô
        pass

def main():
    if len(sys.argv) < 2:
        print("Sử dụng: python3 install_tipa.py <đường_dẫn_tới_file_tipa>")
        sys.exit(1)

    tipa_path = os.path.abspath(sys.argv[1])
    if not os.path.exists(tipa_path):
        print(f"[-] Lỗi: File không tồn tại tại: {tipa_path}")
        sys.exit(1)

    directory = os.path.dirname(tipa_path)
    file_name = os.path.basename(tipa_path)
    port = 8082
    local_ip = get_local_ip()

    print("[*] Đang khởi động HTTP Server nội bộ...")
    server = start_server(directory, port)
    
    install_url = f"apple-magnifier://install?url=http://{local_ip}:{port}/{file_name}"
    
    print("\n==================================================")
    print("🔥 SKILL TỰ ĐỘNG CÀI ĐẶT IPA/TIPA QUA TROLLSTORE 🔥")
    print("==================================================")
    print(f"\n👉 Đường dẫn cài đặt trực tiếp:")
    print(f"\n   {install_url}\n")
    print("--------------------------------------------------")
    print("[*] Bạn có thể quét mã QR dưới đây bằng Camera iPhone để mở link:")
    show_qr(install_url)
    print("--------------------------------------------------")
    print("[*] Đang thử tự động gửi lệnh kích hoạt lên iPhone qua cáp...")

    # Thử tự động gửi link cài đặt lên iPhone bằng pymobiledevice3
    try:
        # Kiểm tra xem có thiết bị nào đang kết nối không
        # Dùng pymobiledevice3 webinspector launch để mở Safari trên iPhone với URL của chúng ta
        # Phải dùng python chạy pymobiledevice3 để đảm bảo tìm thấy binary
        subprocess.run(
            ["python3", "-m", "pymobiledevice3", "webinspector", "launch", install_url],
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
            timeout=5
        )
        print("[+] Đã gửi lệnh mở liên kết cài đặt lên thiết bị thành công!")
        print("[+] Màn hình iPhone sẽ tự động mở Safari và kích hoạt cài đặt qua TrollStore.")
    except subprocess.TimeoutExpired:
        print("[-] Gửi lệnh tự động hết hạn (Timeout). Đảm bảo thiết bị đã tin cậy máy tính.")
    except Exception as e:
        print(f"[-] Không thể tự động gửi lệnh: {e}")
        print("[*] Hãy copy link hoặc quét mã QR ở trên bằng điện thoại để cài đặt thủ công.")

    print("\n[*] Giữ Terminal này hoạt động để iPhone tải file...")
    print("[*] Nhấn Ctrl+C để dừng server khi đã cài đặt xong.")
    
    try:
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        print("\n[*] Đang tắt server...")
        server.shutdown()
        server.server_close()
        print("[+] Đã dừng!")

if __name__ == "__main__":
    main()
