#!/usr/bin/env python3
"""本地 WebGL 构建预览服务（Unity WebGL / TapTap 小游戏调试用）。

为什么要专用脚本：
  Unity 的 Gzip/Brotli 压缩构建产物是 xxx.wasm.gz / xxx.data.gz，
  普通静态服务器不会带 Content-Encoding 头，浏览器会当成二进制下载/解析失败。
  这里把 .gz 映射成 Content-Encoding: gzip、.br 映射成 br，并按内层扩展名给正确的 MIME。

用法：
  python serve_webgl.py <WebGL构建目录> [--port 8000] [--host 127.0.0.1] [--no-open]

示例：
  python serve_webgl.py D:\\git_workSpace\\MirrorWaveGame\\Builds\\WebGL --port 8700
"""

import argparse
import http.server
import os
import socket
import sys
import threading
import webbrowser

EXTRA_MIME = {
    ".wasm": "application/wasm",
    ".data": "application/octet-stream",
    ".js": "text/javascript",
    ".mjs": "text/javascript",
    ".json": "application/json",
}

COMPRESSED_SUFFIX = {".gz": "gzip", ".br": "br"}


class WebGLHandler(http.server.SimpleHTTPRequestHandler):
    def guess_type(self, path):
        lowered = path.lower()
        for suffix in COMPRESSED_SUFFIX:
            if lowered.endswith(suffix):
                lowered = lowered[: -len(suffix)]
                break
        ext = os.path.splitext(lowered)[1]
        if ext in EXTRA_MIME:
            return EXTRA_MIME[ext]
        return super().guess_type(lowered)

    def end_headers(self):
        path = self.path.split("?", 1)[0].lower()
        for suffix, encoding in COMPRESSED_SUFFIX.items():
            if path.endswith(suffix):
                self.send_header("Content-Encoding", encoding)
                break
        # 每次构建都换文件名，但仍禁用缓存，避免调试时吃到旧产物
        self.send_header("Cache-Control", "no-store")
        super().end_headers()

    def log_message(self, fmt, *args):
        sys.stdout.write("  %s\n" % (fmt % args))


def find_free_port(host, start):
    port = start
    while port < start + 100:
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
            sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            try:
                sock.bind((host, port))
                return port
            except OSError:
                port += 1
    raise SystemExit("找不到可用端口（%d-%d）" % (start, start + 99))


def main():
    parser = argparse.ArgumentParser(description="本地 WebGL 构建预览服务")
    parser.add_argument("build_dir", help="Unity WebGL 构建输出目录（含 index.html）")
    parser.add_argument("--port", type=int, default=8000)
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--no-open", action="store_true", help="不自动打开浏览器")
    args = parser.parse_args()

    build_dir = os.path.abspath(args.build_dir)
    if not os.path.isfile(os.path.join(build_dir, "index.html")):
        raise SystemExit("目录里没有 index.html，确认传的是 WebGL 构建目录：%s" % build_dir)

    port = find_free_port(args.host, args.port)
    handler = lambda *a, **kw: WebGLHandler(*a, directory=build_dir, **kw)  # noqa: E731
    server = http.server.ThreadingHTTPServer((args.host, port), handler)

    url = "http://%s:%d/" % (args.host, port)
    print("WebGL 预览：%s" % url)
    print("目录：%s" % build_dir)
    print("Ctrl+C 停止\n")

    if not args.no_open:
        threading.Timer(0.6, lambda: webbrowser.open(url)).start()

    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\n已停止")
    finally:
        server.server_close()


if __name__ == "__main__":
    main()
