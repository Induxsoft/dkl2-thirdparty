# Servidor HTTP local para las pruebas de html.from_url (solo 127.0.0.1).
# Sirve www/ y además: /eco (devuelve los encabezados recibidos), /redir (302 a /index.html),
# /lento (tarda 5 s) y cualquier otra ruta inexistente responde 404.
import http.server, os, sys, time

class H(http.server.SimpleHTTPRequestHandler):
    def __init__(self, *a, **k):
        super().__init__(*a, directory=os.path.join(os.path.dirname(os.path.abspath(__file__)), "www"), **k)
    def log_message(self, *a):
        pass
    def do_GET(self):
        if self.path == "/eco":
            cuerpo = "<html><head><title>eco</title></head><body>"
            for k in sorted(self.headers.keys(), key=str.lower):
                if k.lower() in ("x-prueba", "user-agent", "accept", "authorization"):
                    cuerpo += "<p class='h'>%s=%s</p>" % (k.lower(), self.headers[k])
            cuerpo += "</body></html>"
            b = cuerpo.encode("utf-8")
            self.send_response(200); self.send_header("Content-Type", "text/html; charset=utf-8")
            self.send_header("Content-Length", str(len(b))); self.end_headers(); self.wfile.write(b); return
        if self.path == "/redir":
            self.send_response(302); self.send_header("Location", "/index.html"); self.send_header("Content-Length", "0"); self.end_headers(); return
        if self.path == "/lento":
            time.sleep(5)
        return super().do_GET()

http.server.ThreadingHTTPServer.allow_reuse_address = True
srv = http.server.ThreadingHTTPServer(("127.0.0.1", int(sys.argv[1])), H)
srv.serve_forever()
