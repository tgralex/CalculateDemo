const { env } = require('process');

const target = env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
    env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'https://localhost:7299';

const PROXY_CONFIG = [
  {
    //"/api/*": {
    //  "target": "https://localhost:7299",
    //  "secure": false,
    //  "changeOrigin": true
    //}
    context: [
      "/calculate",
    ],
    target: "https://localhost:7299",
    secure: false
  }
]

module.exports = PROXY_CONFIG;
