#!/usr/bin/env python3
import tkinter as tk
from tkinter import ttk, scrolledtext, filedialog, messagebox, Menu
import subprocess
import threading
import os
import sys
import concurrent.futures
import json
import queue
import ctypes
import shlex 
from pathlib import Path

# --- Configuration ---
APP_NAME = "gallery-dl-gui"
if sys.platform == 'win32':
    CONFIG_DIR = os.path.join(os.environ.get('APPDATA', os.path.expanduser('~')), APP_NAME)
else:
    CONFIG_DIR = os.path.join(os.path.expanduser("~"), ".config", APP_NAME)

if not os.path.exists(CONFIG_DIR):
    try:
        os.makedirs(CONFIG_DIR)
    except OSError:
        CONFIG_DIR = os.path.dirname(os.path.abspath(__file__))

SETTINGS_FILE = os.path.join(CONFIG_DIR, "settings.json")
DEFAULT_SETTINGS = {
    "save_dir": os.path.dirname(os.path.abspath(__file__)),
    "cookies_path": "",
    "max_workers": 4,
    "window_width": 850,
    "window_height": 900,
    "flatten_folder": True,
    "use_archive": False,
    "extra_args": ""
}

# --- High DPI Fix (Windows) ---
if sys.platform == 'win32':
    try:
        ctypes.windll.shcore.SetProcessDpiAwareness(1)
    except Exception:
        pass

# --- Drag & Drop Import Check ---
try:
    from tkinterdnd2 import DND_FILES, DND_TEXT, TkinterDnD
    HAS_DND = True
except ImportError:
    HAS_DND = False

class CreateToolTip(object):
    """
    Lightweight Tooltip class for UI hints.
    FIXED: Now handles widgets without a text cursor (Buttons/Checkboxes).
    """
    def __init__(self, widget, text='widget info'):
        self.wait_time = 500 
        self.wrap_length = 250
        self.widget = widget
        self.text = text
        self.widget.bind("<Enter>", self.enter)
        self.widget.bind("<Leave>", self.leave)
        self.widget.bind("<ButtonPress>", self.leave)
        self.id = None
        self.tw = None

    def enter(self, event=None):
        self.schedule()

    def leave(self, event=None):
        self.unschedule()
        self.hidetip()

    def schedule(self):
        self.unschedule()
        self.id = self.widget.after(self.wait_time, self.showtip)

    def unschedule(self):
        id = self.id
        self.id = None
        if id: self.widget.after_cancel(id)

    def showtip(self, event=None):
        x = y = 0
        # Attempt to calculate position based on text cursor (for Text/Entry widgets)
        try:
            bbox = self.widget.bbox("insert")
            if bbox:
                # If we found a cursor, place tooltip near it
                x, y, cx, cy = bbox
                x += self.widget.winfo_rootx() + 25
                y += self.widget.winfo_rooty() + 20
            else:
                # If bbox returned None (cursor not visible), force fallback
                raise ValueError("bbox is None")
        except (AttributeError, ValueError, tk.TclError):
            # Fallback for Buttons, Checkboxes, or funky Text widgets
            # Place tooltip at bottom-left of the widget itself
            x = self.widget.winfo_rootx() + 20
            y = self.widget.winfo_rooty() + self.widget.winfo_height() + 5

        # Create the tooltip window
        self.tw = tk.Toplevel(self.widget)
        self.tw.wm_overrideredirect(True)
        self.tw.wm_geometry("+%d+%d" % (x, y))
        label = tk.Label(self.tw, text=self.text, justify='left',
                       background="#ffffe0", relief='solid', borderwidth=1,
                       wraplength=self.wrap_length)
        label.pack(ipadx=1)

    def hidetip(self):
        tw = self.tw
        self.tw= None
        if tw: tw.destroy()

class GalleryDLGUI:
    def __init__(self, root):
        self.root = root
        self.settings = self.load_settings()
        
        self.root.title("gallery-dl GUI")
        self.apply_dark_title_bar()
        
        w = self.settings.get("window_width", 850)
        h = self.settings.get("window_height", 900)
        self.root.geometry(f"{w}x{h}")
        
        self.root.protocol("WM_DELETE_WINDOW", self.on_close)
        
        # Shortcuts
        self.root.bind('<Control-Return>', lambda e: self.start_batch_processing())
        self.root.bind('<Escape>', lambda e: self.stop_batch())

        self.root.columnconfigure(1, weight=1)
        self.root.rowconfigure(6, weight=1) 

        style = ttk.Style()
        style.configure("TButton", padding=5)
        
        # --- Menu Bar ---
        menubar = Menu(root)
        filemenu = Menu(menubar, tearoff=0)
        filemenu.add_command(label="Open Config File", command=self.open_config_file)
        filemenu.add_separator()
        filemenu.add_command(label="Update gallery-dl", command=lambda: self.install_package("gallery-dl"))
        filemenu.add_separator()
        filemenu.add_command(label="Exit", command=self.on_close)
        menubar.add_cascade(label="File", menu=filemenu)
        
        helpmenu = Menu(menubar, tearoff=0)
        helpmenu.add_command(label="Install Drag & Drop Support", command=lambda: self.install_package("tkinterdnd2"))
        menubar.add_cascade(label="Help", menu=helpmenu)
        root.config(menu=menubar)

        # --- Row 0: URL Input ---
        top_frame = ttk.Frame(root)
        top_frame.grid(row=0, column=0, padx=10, pady=10, sticky="nw")
        
        ttk.Label(top_frame, text="URLs:", justify=tk.LEFT).pack(anchor="w")
        
        self.watch_clipboard_var = tk.BooleanVar(value=False)
        self.chk_watch = ttk.Checkbutton(top_frame, text="Watch Clipboard", variable=self.watch_clipboard_var, command=self.toggle_watch)
        self.chk_watch.pack(anchor="w", pady=5)
        CreateToolTip(self.chk_watch, "Automatically adds (unique) URLs copied to clipboard.")
        
        self.url_input = scrolledtext.ScrolledText(root, height=8, width=40)
        self.url_input.grid(row=0, column=1, padx=5, pady=10, sticky="ew")
        CreateToolTip(self.url_input, "Paste URLs here.\nDuplicates are removed automatically.\nShortcuts: Ctrl+Enter to Start, Esc to Stop.")
        
        self.url_input.bind('<KeyRelease>', self.update_queue_counter)
        self.url_input.focus_set()

        self.context_menu = Menu(self.root, tearoff=0)
        self.context_menu.add_command(label="Cut", command=lambda: self.url_input.event_generate("<<Cut>>"))
        self.context_menu.add_command(label="Copy", command=lambda: self.url_input.event_generate("<<Copy>>"))
        self.context_menu.add_command(label="Paste", command=lambda: self.paste_from_clipboard()) 
        self.url_input.bind("<Button-3>", self.show_context_menu)

        if HAS_DND:
            self.url_input.drop_target_register(DND_TEXT, DND_FILES)
            self.url_input.dnd_bind('<<Drop>>', self.on_drop)

        btn_frame = ttk.Frame(root)
        btn_frame.grid(row=0, column=2, padx=10, pady=10, sticky="n")
        
        self.btn_paste = ttk.Button(btn_frame, text="Paste", command=self.paste_from_clipboard)
        self.btn_paste.pack(fill=tk.X)
        
        self.btn_clear_urls = ttk.Button(btn_frame, text="Clear", command=self.clear_urls)
        self.btn_clear_urls.pack(pady=5, fill=tk.X)

        # --- Row 1: Destination ---
        ttk.Label(root, text="Save to:").grid(row=1, column=0, padx=10, pady=5, sticky="w")
        self.dir_var = tk.StringVar(value=self.settings["save_dir"]) 
        self.dir_entry = ttk.Entry(root, textvariable=self.dir_var)
        self.dir_entry.grid(row=1, column=1, padx=5, pady=5, sticky="ew")
        self.btn_browse = ttk.Button(root, text="Browse", command=self.browse_directory)
        self.btn_browse.grid(row=1, column=2, padx=10, pady=5)

        # --- Row 2: Cookies ---
        ttk.Label(root, text="Cookies (txt):").grid(row=2, column=0, padx=10, pady=5, sticky="w")
        self.cookies_var = tk.StringVar(value=self.settings.get("cookies_path", ""))
        self.cookies_entry = ttk.Entry(root, textvariable=self.cookies_var)
        self.cookies_entry.grid(row=2, column=1, padx=5, pady=5, sticky="ew")
        self.btn_cookies = ttk.Button(root, text="Select File", command=self.browse_cookies)
        self.btn_cookies.grid(row=2, column=2, padx=10, pady=5)

        # --- Row 3: Advanced Options ---
        adv_frame = ttk.LabelFrame(root, text="Advanced Options")
        adv_frame.grid(row=3, column=0, columnspan=3, padx=10, pady=5, sticky="ew")
        
        self.archive_var = tk.BooleanVar(value=self.settings.get("use_archive", False))
        self.chk_archive = ttk.Checkbutton(adv_frame, text="Track History (Archive)", variable=self.archive_var)
        self.chk_archive.pack(side=tk.LEFT, padx=10, pady=5)
        CreateToolTip(self.chk_archive, "Creates a database file.\nPrevents re-downloading files you already have.")

        self.flatten_var = tk.BooleanVar(value=self.settings.get("flatten_folder", True))
        self.chk_flatten = ttk.Checkbutton(adv_frame, text="Flatten Folder", variable=self.flatten_var)
        self.chk_flatten.pack(side=tk.LEFT, padx=10, pady=5)
        CreateToolTip(self.chk_flatten, "Checked: Files go to 'Save to' folder.\nUnchecked: Creates subfolders.")
        
        ttk.Label(adv_frame, text="Extra Args:").pack(side=tk.LEFT, padx=(20, 5))
        self.extra_args_var = tk.StringVar(value=self.settings.get("extra_args", ""))
        self.entry_args = ttk.Entry(adv_frame, textvariable=self.extra_args_var, width=30)
        self.entry_args.pack(side=tk.LEFT, padx=5, fill=tk.X, expand=True)
        CreateToolTip(self.entry_args, "Pass raw flags to gallery-dl (e.g. --range 1-10).")

        # --- Row 4: Actions ---
        self.action_frame = ttk.Frame(root)
        self.action_frame.grid(row=4, column=0, columnspan=3, pady=15)
        
        ttk.Label(self.action_frame, text="Workers:").pack(side=tk.LEFT, padx=5)
        self.worker_count = tk.Spinbox(self.action_frame, from_=1, to=16, width=5)
        self.worker_count.pack(side=tk.LEFT, padx=5)
        self.worker_count.delete(0, "end")
        self.worker_count.insert(0, self.settings.get("max_workers", 4))
        
        self.btn_download = ttk.Button(self.action_frame, text="Start (Ctrl+Enter)", command=self.start_batch_processing)
        self.btn_download.pack(side=tk.LEFT, padx=(15, 5))

        self.btn_retry = ttk.Button(self.action_frame, text="Retry Failed", command=self.retry_failed, state='disabled')
        self.btn_retry.pack(side=tk.LEFT, padx=5)

        self.btn_stop = ttk.Button(self.action_frame, text="Stop (Esc)", command=self.stop_batch, state='disabled')
        self.btn_stop.pack(side=tk.LEFT, padx=5)
        
        self.btn_open_dir = ttk.Button(self.action_frame, text="Open Folder", command=self.open_directory)
        self.btn_open_dir.pack(side=tk.LEFT, padx=(20, 5))
        
        self.btn_clear_log = ttk.Button(self.action_frame, text="Clear Console", command=self.clear_log)
        self.btn_clear_log.pack(side=tk.LEFT, padx=5)

        # --- Row 5: Progress Bar ---
        self.progress_var = tk.DoubleVar()
        self.progress_bar = ttk.Progressbar(root, variable=self.progress_var, maximum=100)
        self.progress_bar.grid(row=5, column=0, columnspan=3, padx=10, pady=(0, 10), sticky="ew")

        # --- Row 6: Console ---
        self.output_text = scrolledtext.ScrolledText(root, state='disabled', height=15, bg="#1e1e1e", fg="#d4d4d4", font=("Consolas", 9))
        self.output_text.grid(row=6, column=0, columnspan=3, padx=10, pady=(0, 10), sticky="nsew")
        
        self.output_text.tag_config("INFO", foreground="#569cd6")  
        self.output_text.tag_config("ERROR", foreground="#f44747") 
        self.output_text.tag_config("SUCCESS", foreground="#4ec9b0")
        self.output_text.tag_config("CMD", foreground="#ce9178") 
        self.output_text.tag_config("THREAD", foreground="#c586c0") 
        self.output_text.tag_config("SYSTEM", foreground="#dcdcaa")

        # --- Row 7: Status Bar ---
        self.status_var = tk.StringVar(value="Ready")
        self.status_bar = ttk.Label(root, textvariable=self.status_var, relief=tk.SUNKEN, anchor=tk.W)
        self.status_bar.grid(row=7, column=0, columnspan=3, sticky="ew")

        self.is_downloading = False
        self.stop_requested = False
        self.failed_urls = []
        self.log_queue = queue.Queue()
        self.check_log_queue()
        
        # Init Watch Variables
        self.last_clipboard_content = ""
        self.root.after(1000, self.clipboard_watcher_loop)
        
        # Run startup checks
        self.root.after(500, self.check_system_health)

    # --- Aesthetics ---
    def apply_dark_title_bar(self):
        try:
            self.root.update()
            set_window_attribute = ctypes.windll.dwmapi.DwmSetWindowAttribute
            get_parent = ctypes.windll.user32.GetParent
            hwnd = get_parent(self.root.winfo_id())
            value = ctypes.c_int(2) 
            set_window_attribute(hwnd, 20, ctypes.byref(value), ctypes.sizeof(value))
        except: pass

    # --- Settings ---
    def load_settings(self):
        if os.path.exists(SETTINGS_FILE):
            try:
                with open(SETTINGS_FILE, "r") as f: return json.load(f)
            except: return DEFAULT_SETTINGS.copy()
        return DEFAULT_SETTINGS.copy()

    def save_settings(self):
        try: mw = int(self.worker_count.get())
        except: mw = 4
        data = {
            "save_dir": self.dir_var.get(),
            "cookies_path": self.cookies_var.get(),
            "max_workers": mw,
            "window_width": self.root.winfo_width(),
            "window_height": self.root.winfo_height(),
            "flatten_folder": self.flatten_var.get(),
            "use_archive": self.archive_var.get(),
            "extra_args": self.extra_args_var.get()
        }
        with open(SETTINGS_FILE, "w") as f:
            json.dump(data, f)

    def on_close(self):
        self.save_settings()
        self.root.destroy()

    # --- Clipboard Watcher ---
    def toggle_watch(self):
        if self.watch_clipboard_var.get(): self.status_var.set("Clipboard Watcher Active")
        else: self.status_var.set("Ready")

    def clipboard_watcher_loop(self):
        if self.watch_clipboard_var.get():
            try:
                content = self.root.clipboard_get()
                if content != self.last_clipboard_content:
                    self.last_clipboard_content = content
                    if content.startswith("http"):
                        added = self.append_url_text(content)
                        if added: self.url_input.see(tk.END)
            except: pass
        self.root.after(1000, self.clipboard_watcher_loop)

    # --- System Checks ---
    def check_system_health(self):
        if not HAS_DND:
            self.log("Tip: Install 'tkinterdnd2' via Help menu for drag-and-drop.", "SYSTEM")
        def run_check():
            try:
                subprocess.check_output(["gallery-dl", "--version"], stderr=subprocess.STDOUT, startupinfo=self.get_startup_info())
                self.log("System Check: gallery-dl found.", "SYSTEM")
            except FileNotFoundError:
                self.log("CRITICAL ERROR: 'gallery-dl' not found in PATH!", "ERROR")
                self.root.after(0, lambda: messagebox.showerror("Missing Dependency", "gallery-dl was not found.\nPlease install it."))
        threading.Thread(target=run_check, daemon=True).start()

    def get_startup_info(self):
        if sys.platform == 'win32':
            si = subprocess.STARTUPINFO()
            si.dwFlags |= subprocess.STARTF_USESHOWWINDOW
            return si
        return None

    # --- UI Interactions ---
    def show_context_menu(self, event):
        self.context_menu.tk_popup(event.x_root, event.y_root)

    def update_queue_counter(self, event=None):
        raw_text = self.url_input.get("1.0", tk.END)
        lines = [line.strip() for line in raw_text.splitlines() if line.strip()]
        if not self.is_downloading:
            self.status_var.set(f"Queue: {len(lines)} URLs ready")

    def append_url_text(self, text):
        text = text.strip()
        if not text: return False
        
        current_content = self.url_input.get("1.0", tk.END).strip()
        existing_lines = set(line.strip() for line in current_content.splitlines() if line.strip())
        
        new_lines = []
        for line in text.splitlines():
            clean = line.strip()
            if clean and clean not in existing_lines:
                new_lines.append(clean)
                existing_lines.add(clean)
        
        if not new_lines: return False 
            
        if len(current_content) > 0:
            self.url_input.insert(tk.END, "\n")
            
        for line in new_lines:
            self.url_input.insert(tk.END, line + "\n")
            
        self.update_queue_counter()
        return True

    def paste_from_clipboard(self):
        try:
            text = self.root.clipboard_get()
            self.append_url_text(text)
        except tk.TclError: pass 

    def on_drop(self, event):
        if event.data:
            clean_data = event.data.strip('{}')
            self.append_url_text(clean_data)

    def clear_urls(self):
        self.url_input.delete("1.0", tk.END)
        self.update_queue_counter()

    def retry_failed(self):
        if not self.failed_urls: return
        self.clear_urls()
        for url in self.failed_urls:
            self.append_url_text(url)
        self.failed_urls = [] 
        self.btn_retry.config(state='disabled')
        self.log("Failed URLs re-queued. Click Start to try again.", "INFO")

    def browse_directory(self):
        d = filedialog.askdirectory()
        if d: self.dir_var.set(d)

    def browse_cookies(self):
        f = filedialog.askopenfilename(filetypes=[("Text Files", "*.txt"), ("All", "*.*")])
        if f: self.cookies_var.set(f)

    def open_directory(self):
        path = self.dir_var.get()
        if os.path.exists(path):
            if sys.platform == 'win32': os.startfile(path)
            elif sys.platform == 'darwin': subprocess.Popen(['open', path])
            else: subprocess.Popen(['xdg-open', path])

    def open_config_file(self):
        home = Path.home()
        if sys.platform == 'win32': config_path = home / "gallery-dl.conf"
        else: config_path = home / "/etc/gallery-dl.conf"
        if not config_path.exists():
            try:
                with open(config_path, "w") as f: f.write("{}")
            except: pass
        if sys.platform == 'win32': os.startfile(config_path)
        elif sys.platform == 'darwin': subprocess.Popen(['open', config_path])
        else: subprocess.Popen(['xdg-open', config_path])

    def install_package(self, package_name):
        self.log(f"Installing/Updating {package_name}...", "SYSTEM")
        def run_install():
            try:
                cmd = [sys.executable, "-m", "pip", "install", "--upgrade", package_name]
                subprocess.check_call(cmd, startupinfo=self.get_startup_info())
                self.log(f"{package_name} installed successfully! Restart App.", "SUCCESS")
                self.root.after(0, lambda: messagebox.showinfo("Success", f"{package_name} installed.\nPlease restart the app."))
            except Exception as e:
                self.log(f"Installation failed: {e}", "ERROR")
        threading.Thread(target=run_install, daemon=True).start()

    # --- Logging ---
    def log(self, message, tag=None):
        self.log_queue.put((message, tag))

    def check_log_queue(self):
        while not self.log_queue.empty():
            msg, tag = self.log_queue.get()
            self.output_text.config(state='normal')
            self.output_text.insert(tk.END, msg + "\n", tag)
            self.output_text.see(tk.END)
            self.output_text.config(state='disabled')
        self.root.after(100, self.check_log_queue)

    def clear_log(self):
        self.output_text.config(state='normal')
        self.output_text.delete(1.0, tk.END)
        self.output_text.config(state='disabled')

    # --- Core Logic ---
    def stop_batch(self):
        if self.is_downloading:
            self.stop_requested = True
            self.btn_stop.config(state='disabled', text="Stopping...")
            self.log("--- Stop Requested: Finishing active downloads... ---", "ERROR")

    def start_batch_processing(self):
        if self.is_downloading: return

        raw_text = self.url_input.get("1.0", tk.END)
        urls = [line.strip() for line in raw_text.splitlines() if line.strip()]
        dest = self.dir_var.get().strip()
        cookies = self.cookies_var.get().strip()
        flatten = self.flatten_var.get()
        use_archive = self.archive_var.get()
        extra_args = self.extra_args_var.get().strip()
        
        if not urls:
            messagebox.showwarning("Input Error", "Please enter at least one URL.")
            return

        try: max_workers = int(self.worker_count.get())
        except ValueError: max_workers = 4

        self.is_downloading = True
        self.stop_requested = False
        self.failed_urls = [] 
        self.btn_retry.config(state='disabled')
        
        self.btn_download.config(state='disabled', text="Running...")
        self.btn_stop.config(state='normal', text="Stop")
        self.status_var.set(f"Queue: {len(urls)} | Workers: {max_workers}")
        self.progress_var.set(0)
        self.log(f"--- Starting Batch ({len(urls)} URLs) ---", "INFO")
        
        threading.Thread(target=self.run_thread_pool, args=(urls, dest, cookies, flatten, use_archive, extra_args, max_workers), daemon=True).start()

    def run_thread_pool(self, urls, dest, cookies, flatten, use_archive, extra_args, max_workers):
        total = len(urls)
        completed = 0
        
        with concurrent.futures.ThreadPoolExecutor(max_workers=max_workers) as executor:
            futures = {executor.submit(self.download_single_url, url, dest, cookies, flatten, use_archive, extra_args): url for url in urls}
            
            for future in concurrent.futures.as_completed(futures):
                url = futures[future]
                completed += 1
                perc = (completed / total) * 100
                self.root.after(0, lambda p=perc: self.progress_var.set(p))
                self.root.after(0, lambda c=completed, t=total: self.status_var.set(f"Progress: {c}/{t}"))
                
                try:
                    if not future.result(): self.failed_urls.append(url)
                except Exception as e:
                    self.log(f"Exception for {url}: {e}", "ERROR")
                    self.failed_urls.append(url)
                    
                if self.stop_requested: break 

        if self.stop_requested:
            self.log("--- Batch Stopped by User ---", "ERROR")
        else:
            self.log("-" * 40)
            if self.failed_urls:
                self.log(f"--- Finished with {len(self.failed_urls)} Errors ---", "ERROR")
                self.root.after(0, lambda: self.btn_retry.config(state='normal'))
            else:
                self.log("--- All Downloads Finished Successfully ---", "SUCCESS")
                self.root.after(0, lambda: self.progress_var.set(100))
            
            if sys.platform == 'win32':
                try:
                    import winsound
                    winsound.MessageBeep(winsound.MB_ICONASTERISK)
                except: pass
        
        self.root.after(0, lambda: self.status_var.set("Ready"))
        self.root.after(0, lambda: self.btn_download.config(state='normal', text="Start"))
        self.root.after(0, lambda: self.btn_stop.config(state='disabled', text="Stop"))
        self.is_downloading = False

    def download_single_url(self, url, dest, cookies, flatten, use_archive, extra_args):
        if self.stop_requested: return False

        cmd = ["gallery-dl", "--dest", dest]
        if flatten: cmd.extend(["--directory", "."])
        if cookies and os.path.exists(cookies): cmd.extend(["--cookies", cookies])
        
        if use_archive:
            archive_path = os.path.join(dest, "archive.sqlite")
            cmd.extend(["--download-archive", archive_path])

        if extra_args:
            try: cmd.extend(shlex.split(extra_args))
            except ValueError:
                self.log(f"Error: Invalid Extra Args syntax (mismatched quotes?)", "ERROR")
                return False

        cmd.append(url)
        
        t_name = threading.current_thread().name
        short_name = t_name.replace("Thread", "W") 
        
        self.log(f"[{short_name}] Starting: {url}", "THREAD")
        
        try:
            process = subprocess.Popen(
                cmd, 
                stdout=subprocess.PIPE, 
                stderr=subprocess.STDOUT, 
                text=True,
                bufsize=1,
                universal_newlines=True,
                startupinfo=self.get_startup_info()
            )

            for line in process.stdout:
                line = line.strip()
                if line:
                    if line.startswith("#"): self.log(f"[{short_name}] {line}")
                    elif "http" in line or "File" in line: pass 
                    else: self.log(f"[{short_name}] {line}")
            
            process.wait()
            
            if process.returncode == 0:
                self.log(f"[{short_name}] Finished", "SUCCESS")
                return True
            else:
                self.log(f"[{short_name}] Error ({process.returncode})", "ERROR")
                return False

        except Exception as e:
            self.log(f"[{short_name}] Critical Error: {str(e)}", "ERROR")
            return False

if __name__ == "__main__":
    if HAS_DND:
        root = TkinterDnD.Tk()
    else:
        root = tk.Tk()
    app = GalleryDLGUI(root)
    root.mainloop()