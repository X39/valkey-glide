# Start from the official Valkey image
FROM valkey/valkey:latest

# Install curl and Python (for the healthcheck API)
RUN apt-get update && \
    apt-get install -y curl python3 python3-pip && \
    pip3 install flask --break-system-packages && \
    apt-get clean

# Create a simple Flask app to expose /health
COPY healthcheck.py /opt/healthcheck.py

# Expose health check port
EXPOSE 8080

# Run both Valkey and the healthcheck API
CMD bash -c "\
    valkey-server & \
    python3 /opt/healthcheck.py"
