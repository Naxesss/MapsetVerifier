const quote = (files) => files.map((f) => `"${f}"`).join(" ");

module.exports = {
  "electron-app/src/**/*.{ts,tsx}": (files) => [
    `node electron-app/node_modules/prettier/bin/prettier.cjs --write ${quote(files)}`,
    `node electron-app/node_modules/eslint/bin/eslint.js --config electron-app/eslint.config.cjs --fix ${quote(files)}`,
  ],
  "electron-app/src/**/*.{css,scss,md}": (files) =>
    `node electron-app/node_modules/prettier/bin/prettier.cjs --write ${quote(files)}`,
  "**/*.cs": (files) => `dotnet csharpier format ${quote(files)}`,
};
