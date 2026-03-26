FROM node:20-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm install --legacy-peer-deps

COPY . .
RUN npx ng build --configuration=production

# Debug - listar TODA a estrutura de dist
RUN find dist/ -type f -name "index.html" && echo "---" && ls -R dist/ | head -50

FROM nginx:alpine

# Tentar copiar de vários possíveis caminhos
COPY --from=build /app/dist/Materialpro/browser /usr/share/nginx/html

# Se browser/ não tiver index.html, copiar de Materialpro/ direto
RUN if [ ! -f /usr/share/nginx/html/index.html ]; then echo "index.html not in browser/, trying root..."; fi
RUN ls -la /usr/share/nginx/html/ | head -20

# Config nginx simples
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

RUN printf '#!/bin/sh\nsed -i "s/PORT_PLACEHOLDER/${PORT:-80}/" /etc/nginx/conf.d/default.conf\ncat /etc/nginx/conf.d/default.conf\nnginx -g "daemon off;"\n' > /start.sh && chmod +x /start.sh

CMD ["/start.sh"]
