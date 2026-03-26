FROM node:20-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm install --legacy-peer-deps

COPY . .
RUN npx ng build --configuration=production

FROM nginx:alpine

COPY --from=build /app/dist/Materialpro/browser /usr/share/nginx/html

# Nginx config template com PORT variável
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

# Entrypoint que substitui a porta
RUN printf '#!/bin/sh\nsed -i "s/PORT_PLACEHOLDER/${PORT:-80}/" /etc/nginx/conf.d/default.conf\nnginx -g "daemon off;"\n' > /start.sh && chmod +x /start.sh

CMD ["/start.sh"]
