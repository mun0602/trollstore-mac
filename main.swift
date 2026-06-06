import SwiftUI
import Foundation
import AppKit

@main
struct MunAIApp: App {
    var body: some Scene {
        WindowGroup {
            ContentView()
        }
        .windowStyle(.hiddenTitleBar)
    }
}

struct ContentView: View {
    @State private var isLoggedIn: Bool = false
    @State private var logoImage: NSImage? = nil

    var body: some View {
        Group {
            if !isLoggedIn {
                LoginView(isLoggedIn: $isLoggedIn, logoImage: logoImage)
            } else {
                InstallerView(logoImage: logoImage)
            }
        }
        .frame(width: 580, height: 490)
        .background(Color(red: 0.1, green: 0.1, blue: 0.12))
        .onAppear(perform: loadLogo)
    }

    func loadLogo() {
        if let logoPath = Bundle.main.path(forResource: "icon", ofType: "png") {
            self.logoImage = NSImage(contentsOfFile: logoPath)
        }
    }
}

struct LoginView: View {
    @Binding var isLoggedIn: Bool
    var logoImage: NSImage?
    
    @State private var passwordInput: String = ""
    @State private var showError: Bool = false

    var body: some View {
        VStack(spacing: 20) {
            Spacer()
            
            // Logo
            if let logo = logoImage {
                Image(nsImage: logo)
                    .resizable()
                    .aspectRatio(contentMode: .fit)
                    .frame(width: 100, height: 100)
                    .cornerRadius(20)
            } else {
                Image(systemName: "lock.iphone")
                    .resizable()
                    .aspectRatio(contentMode: .fit)
                    .frame(width: 100, height: 100)
                    .foregroundColor(.green)
            }

            VStack(spacing: 6) {
                Text("mun-ai")
                    .font(.system(size: 28, weight: .bold, design: .rounded))
                    .foregroundColor(.white)
                Text("Hệ thống xác thực thiết bị")
                    .font(.system(size: 13))
                    .foregroundColor(.gray)
            }
            .padding(.bottom, 20)

            // Password Field
            VStack(alignment: .leading, spacing: 8) {
                Text("Mật khẩu truy cập:")
                    .font(.system(size: 12, weight: .semibold))
                    .foregroundColor(.gray)
                
                SecureField("Nhập mật khẩu...", text: $passwordInput, onCommit: checkPassword)
                    .textFieldStyle(PlainTextFieldStyle())
                    .padding(.horizontal, 12)
                    .padding(.vertical, 10)
                    .background(Color.white.opacity(0.08))
                    .cornerRadius(8)
                    .foregroundColor(.white)
                    .frame(width: 250)
            }

            if showError {
                Text("❌ Sai mật khẩu, vui lòng thử lại!")
                    .font(.system(size: 12, weight: .semibold))
                    .foregroundColor(.red)
                    .transition(.opacity)
            }

            // Login Button
            Button(action: checkPassword) {
                Text("Đăng nhập")
                    .font(.system(size: 14, weight: .bold))
                    .foregroundColor(.black)
                    .padding(.horizontal, 40)
                    .padding(.vertical, 10)
                    .background(Color.green)
                    .cornerRadius(8)
            }
            .buttonStyle(PlainButtonStyle())
            .padding(.top, 10)

            Spacer()
        }
    }

    func checkPassword() {
        if passwordInput == "1305" {
            withAnimation {
                isLoggedIn = true
            }
        } else {
            withAnimation {
                showError = true
            }
            passwordInput = ""
        }
    }
}

struct InstallerView: View {
    var logoImage: NSImage?
    
    @State private var appName: String = "Tips"
    @State private var logs: String = ""
    @State private var isRunning: Bool = false
    @State private var isFinished: Bool = false
    @State private var isSuccess: Bool = false

    var body: some View {
        VStack(spacing: 0) {
            // Header
            HStack(spacing: 16) {
                if let logo = logoImage {
                    Image(nsImage: logo)
                        .resizable()
                        .aspectRatio(contentMode: .fit)
                        .frame(width: 64, height: 64)
                        .cornerRadius(12)
                } else {
                    Image(systemName: "lock.iphone")
                        .resizable()
                        .aspectRatio(contentMode: .fit)
                        .frame(width: 64, height: 64)
                        .foregroundColor(.green)
                }

                VStack(alignment: .leading, spacing: 4) {
                    Text("mun-ai")
                        .font(.system(size: 22, weight: .bold, design: .rounded))
                        .foregroundColor(.white)
                    Text("Bộ cài đặt TrollStore tự động và bảo mật")
                        .font(.system(size: 12))
                        .foregroundColor(.gray)
                }
                Spacer()
            }
            .padding(.horizontal, 24)
            .padding(.top, 24)
            .padding(.bottom, 16)
            
            Divider()
                .background(Color.gray.opacity(0.2))

            // Body Config
            VStack(alignment: .leading, spacing: 14) {
                Text("Hướng dẫn nhanh:")
                    .font(.system(size: 13, weight: .bold))
                    .foregroundColor(.gray)

                VStack(alignment: .leading, spacing: 6) {
                    Label("Đảm bảo iPhone/iPad đã kết nối bằng cáp USB và chọn 'Tin cậy' (Trust).", systemImage: "checkmark.circle.fill")
                    Label("Tắt Tìm iPhone (Find My iPhone) trong cài đặt iCloud.", systemImage: "checkmark.circle.fill")
                    Label("Nhập ứng dụng hệ thống sẽ bị thay thế (Khuyên dùng: Tips).", systemImage: "checkmark.circle.fill")
                }
                .font(.system(size: 12))
                .foregroundColor(.white.opacity(0.8))

                HStack(spacing: 12) {
                    Text("Ứng dụng thay thế:")
                        .font(.system(size: 13, weight: .semibold))
                    TextField("Tips", text: $appName)
                        .textFieldStyle(PlainTextFieldStyle())
                        .padding(.horizontal, 10)
                        .padding(.vertical, 6)
                        .background(Color.white.opacity(0.08))
                        .cornerRadius(6)
                        .frame(width: 150)
                        .disabled(isRunning)
                }
                .padding(.top, 6)
            }
            .padding(.horizontal, 24)
            .padding(.vertical, 16)

            // Progress/Indicator
            if isRunning {
                HStack(spacing: 8) {
                    ProgressView()
                        .scaleEffect(0.7)
                    Text("Đang thực hiện cài đặt...")
                        .font(.system(size: 12))
                        .foregroundColor(.green)
                }
                .padding(.horizontal, 24)
                .padding(.bottom, 8)
            }

            // Console Logs
            ScrollView {
                VStack(alignment: .leading, spacing: 4) {
                    Text(logs)
                        .font(.system(size: 11, design: .monospaced))
                        .foregroundColor(.green)
                        .frame(maxWidth: .infinity, alignment: .leading)
                        .textSelection(.enabled)
                }
                .padding(12)
            }
            .background(Color.black.opacity(0.9))
            .cornerRadius(8)
            .padding(.horizontal, 24)
            .padding(.bottom, 16)

            Divider()
                .background(Color.gray.opacity(0.2))

            // Action
            HStack {
                Button(action: copyToClipboard) {
                    HStack(spacing: 6) {
                        Image(systemName: "doc.on.doc")
                        Text("Sao chép Log")
                    }
                    .font(.system(size: 12, weight: .semibold))
                    .foregroundColor(.white.opacity(0.8))
                    .padding(.horizontal, 12)
                    .padding(.vertical, 8)
                    .background(Color.white.opacity(0.08))
                    .cornerRadius(6)
                }
                .buttonStyle(PlainButtonStyle())

                Spacer()
                
                Button(action: startInstallation) {
                    Text(isRunning ? "Đang xử lý..." : "Cài đặt ngay")
                        .font(.system(size: 14, weight: .bold))
                        .foregroundColor(.white)
                        .padding(.horizontal, 24)
                        .padding(.vertical, 8)
                        .background(isRunning ? Color.gray : Color.green)
                        .cornerRadius(8)
                }
                .buttonStyle(PlainButtonStyle())
                .disabled(isRunning)
            }
            .padding(.horizontal, 24)
            .padding(.vertical, 16)
        }
    }

    func copyToClipboard() {
        let pasteboard = NSPasteboard.general
        pasteboard.clearContents()
        pasteboard.setString(logs, forType: .string)
    }

    func startInstallation() {
        isRunning = true
        isFinished = false
        logs = "[*] Khởi động tiến trình cài đặt bằng Native Process...\n"
        
        let process = Process()
        process.executableURL = URL(fileURLWithPath: "/usr/bin/python3")
        
        guard let scriptPath = Bundle.main.path(forResource: "trollstore", ofType: "py") else {
            logs += "[-] Lỗi: Không tìm thấy file trollstore.py trong tài nguyên ứng dụng.\n"
            isRunning = false
            return
        }
        
        process.arguments = [scriptPath]
        
        let outputPipe = Pipe()
        let inputPipe = Pipe()
        
        process.standardOutput = outputPipe
        process.standardError = outputPipe
        process.standardInput = inputPipe
        
        let outHandle = outputPipe.fileHandleForReading
        outHandle.readabilityHandler = { fileHandle in
            let data = fileHandle.availableData
            if let line = String(data: data, encoding: .utf8), !line.isEmpty {
                DispatchQueue.main.async {
                    self.logs += line
                }
            }
        }
        
        process.terminationHandler = { process in
            DispatchQueue.main.async {
                self.isRunning = false
                self.isFinished = true
                self.isSuccess = (process.terminationStatus == 0)
                
                if self.isSuccess {
                    self.logs += "\n[+] CÀI ĐẶT THÀNH CÔNG!"
                    self.logs += "\n[+] Thiết bị của bạn đang tự động khởi động lại..."
                } else {
                    self.logs += "\n[-] Tiến trình lỗi kết thúc với mã: \(process.terminationStatus)\n"
                }
            }
        }
        
        do {
            try process.run()
            
            // Ghi tên app thay thế vào stdin (trollstore.py click.prompt)
            let appInput = appName + "\n"
            if let data = appInput.data(using: .utf8) {
                inputPipe.fileHandleForWriting.write(data)
            }
        } catch {
            logs += "[-] Không thể chạy tiến trình Python: \(error.localizedDescription)\n"
            isRunning = false
        }
    }
}
