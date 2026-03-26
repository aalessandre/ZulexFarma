FROM node:20-alpine AS build
WORKDIR /app

# Forçar rebuild - v3
COPY package*.json ./
RUN npm install --legacy-peer-deps

COPY . .
RUN npx ng build --configuration=production

# Mostrar onde index.html está
RUN echo "=== FIND INDEX ===" && find dist/ -name "index.html" -type f
RUN echo "=== LS DIST ===" && ls -la dist/Materialpro/
RUN echo "=== LS BROWSER ===" && ls -la dist/Materialpro/browser/ 2>/dev/null || echo "No browser dir"

FROM nginx:alpine

# Copiar TUDO do dist para temp
COPY --from=build /app/dist/Materialpro/ /tmp/dist/

# Verificar o que veio
RUN echo "=== TEMP DIST ===" && find /tmp/dist/ -name "index.html" -type f

# Copiar para nginx - testar ambos os caminhos
RUN if [ -f /tmp/dist/browser/index.html ]; then \
      echo "FOUND in browser/"; \
      cp -r /tmp/dist/browser/* /usr/share/nginx/html/; \
    elif [ -f /tmp/dist/index.html ]; then \
      echo "FOUND in root"; \
      cp -r /tmp/dist/* /usr/share/nginx/html/; \
    else \
      echo "NOT FOUND - listing all:"; \
      find /tmp/dist/ -type f | head -20; \
    fi

RUN rm -rf /tmp/dist
RUN echo "=== NGINX HTML ===" && ls /usr/share/nginx/html/ | head -10

# Nginx config
RUN printf 'server {\n\
    listen PORT_PLACEHOLDER;\n\
    server_name _;\n\
    root /usr/share/nginx/html;\n\
    index index.html;\n\
    location / {\n\
        try_files $uri $uri/ /index.html;\n\
    }\n\
    gzip on;\n\
    gzip_types text/plain text/css application/json application/javascript text/xml application/xml text/javascript;\n\
}\n' > /etc/nginx/conf.d/default.conf

RUN printf '#!/bin/sh\nsed -i "s/PORT_PLACEHOLDER/${PORT:-80}/" /etc/nginx/conf.d/default.conf\nnginx -g "daemon off;"\n' > /start.sh && chmod +x /start.sh

CMD ["/start.sh"]
