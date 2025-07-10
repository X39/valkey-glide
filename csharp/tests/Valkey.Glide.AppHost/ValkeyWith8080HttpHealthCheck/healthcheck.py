from flask import Flask
import subprocess

app = Flask(__name__)

@app.route("/health")
def health():
    try:
        result = subprocess.run(["valkey-cli", "PING"], capture_output=True, text=True, timeout=2)
        if result.stdout.strip() == "PONG":
            return "Healthy", 200
        else:
            return "Unhealthy", 500
    except Exception as e:
        return f"Error: {str(e)}", 500

app.run(host="0.0.0.0", port=8080)
