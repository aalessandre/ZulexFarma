FROM node:20-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm install --legacy-peer-deps

COPY . .
RUN npx ng build --configuration=production

FROM nginx:alpine
COPY --from=build /app/dist/Materialpro/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD sh -c "sed -i 's/listen 80/listen ${PORT:-80}/' /etc/nginx/conf.d/default.conf && nginx -g 'daemon off;'"
