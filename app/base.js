// Contains the very core of the application, like window functions and how links should behave

const {remote, shell} = require("electron");
window.$ = window.jQuery = require('jquery');
const utils = require('./utils.js');

const appdataPath =
	process.env.APPDATA ||
	(process.platform == 'darwin' ?
		process.env.HOME + 'Library/Preferences' :
		process.env.HOME + "/.local/share");
const settingsPath = appdataPath + "/Mapset Verifier Externals/settings.ini";

/// Adds functionality to the top buttons, like minimize, maximize, etc.
document.getElementById("top-button-close").addEventListener("click", () => remote.getCurrentWindow().close());
document.getElementById("top-button-maximize").addEventListener("click", () =>
{
	const window = remote.getCurrentWindow();
	window.isMaximized() ? window.unmaximize() : window.maximize();

	utils.saveFile(settingsPath, "maximized", window.isMaximized() ? "1" : "0");
});
document.getElementById("top-button-minimize").addEventListener("click", () => remote.getCurrentWindow().minimize());

/// Opens links in the default browser rather than in the electron one.
$(document).on('click', 'a[href^="http"]', function (event)
{
    event.preventDefault();
    shell.openExternal(this.href);
});

/// Prevents the electron browser from opening separate tabs upon, for example, dropping images.
document.ondrop = (e) =>
{
	e.preventDefault();
	return false;
};

/// Remembers whether the window was maximized or windowed
$(() =>
{
	const maximized = utils.readFile(settingsPath, "maximized");
	if(maximized == "1")
		remote.getCurrentWindow().maximize();
});