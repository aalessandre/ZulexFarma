FROM node:20-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm install --legacy-peer-deps

COPY . .
RUN npx ng build --configuration=production

# Encontrar onde o index.html foi gerado
RUN find dist/ -name "index.html" -type f

FROM nginx:alpine

# Copiar TUDO do dist/Materialpro para nginx html
COPY --from=build /app/dist/Materialpro/ /tmp/dist/

# Mover: se browser/ existe e tem index.html, usar browser/. Senao usar raiz.
RUN if [ -f /tmp/dist/browser/index.html ]; then \
      cp -r /tmp/dist/browser/* /usr/share/nginx/html/; \
    elif [ -f /tmp/dist/index.html ]; then \
      cp -r /tmp/dist/* /usr/share/nginx/html/; \
    fi && \
    rm -rf /tmp/dist && \
    echo "=== Files in html ===" && \
    ls /usr/share/nginx/html/index.html

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
