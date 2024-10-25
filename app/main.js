const {app, BrowserWindow, globalShortcut}	= require("electron");
const {remote, shell} 						= require("electron");
const localShortcut 						= require('electron-localshortcut');
const {autoUpdater} 						= require("electron-updater");
const { ipcMain } 							= require('electron')

const path 	= require("path");
const url 	= require("url");

let mainWindow;

// Assume it is a pre-release if the version ends with a non-number (e.g. 1.1.0-a).
var packageJson = require(__dirname + "/package.json");
var currentVersion = packageJson.version;
var isPreRelease = isNaN(currentVersion.slice(-1));

/// Creates the electron window
function createWindow ()
{
	mainWindow = new BrowserWindow(
	{
		webPreferences: {
			nodeIntegration: true
		},
		width: 1172,
		height: 700,
		minWidth:800,
		minHeight:600,
		frame: false,
		backgroundColor: "#101014",
		icon:"assets/64x64.png",
		show: false
	});
	
	// shows the window once it has completed loading
	mainWindow.once("ready-to-show", () =>
	{
		mainWindow.show();
		checkForUpdates();
		
		// enables keyboard debugging shortcuts
		localShortcut.register(mainWindow, "Ctrl+Shift+I", () => { mainWindow.toggleDevTools(); });
		localShortcut.register(mainWindow, "F12", 	       () => { mainWindow.toggleDevTools(); });
		
		var reloadFunction = () =>
		{
			mainWindow.reload();
			checkForUpdates();
		};
		localShortcut.register(mainWindow, "Ctrl+R", reloadFunction);
		localShortcut.register(mainWindow, "F5", 	 reloadFunction);
	});

	// loads the index.html file to display
	mainWindow.loadURL(url.format(
	{
		pathname: path.join(__dirname, "index.html"),
		protocol: "file:",
		slashes: true
	}));

	// frees memory upon being closed
	mainWindow.on("closed", () =>
	{
		mainWindow = null;
	});
}

// Prevents duplicate instances.
const noLock = app.requestSingleInstanceLock();
if (!noLock)
    app.exit();

app.on('second-instance', (e, argv, cwd) =>
{
	if (mainWindow)
	{
        if (!mainWindow.isVisible())  mainWindow.show();
        if (mainWindow.isMinimized()) mainWindow.restore();
        mainWindow.focus();
    }
});

/// Called once electron has completed loading
app.on("ready", startApi);

/// Quits the application upon closing the window
app.on("window-all-closed", () =>
{
	// On darwin (mac/os x) it's common for apps to still
	// be active in the task bar until explicitly closed
	if (process.platform !== "darwin")
	{
		// Child process lingers sometimes, this may fix that
		if(apiProcess != null)
			apiProcess.kill();
		
		app.quit();
	}
});

/// Recreates the window if it doesn't exist
app.on("activate", () =>
{
	// Usually removes the window when minimized on darwin systems
	if (mainWindow === null)
		createWindow();
});



const os = require('os');
var apiProcess = null;

function startApi()
{
	var childProcess = require("child_process");
	
	// Start the correct backend executable for the current operating system.
	var apipath = path.join(__dirname, "api/win-x86/MapsetVerifier.exe");
	if (os.platform() === "darwin")
		apipath = path.join(__dirname, "api/osx-x64/MapsetVerifier");
	else if (os.platform() === "linux")
		apipath = path.join(__dirname, "api/linux-x64/MapsetVerifier");
	
	// bass.dll needs to be 32-bit for 32-bit programs, which run on both 64-bit and 32-bit windows.
	// If bass.dll would be 64-bit, it would only run for 64-bit programs, which only run on 64-bit windows.
	// Hence, the process that uses bass.dll needs to be 32-bit for this to work on both 64-bit and 32-bit windows.
	
	//else if (process.arch === 'x64' || process.env.hasOwnProperty('PROCESSOR_ARCHITEW6432'))
	//	apipath = path.join(__dirname, "api/win-x64/MapsetVerifierBackend.exe");
	
	try
	{
		apiProcess = childProcess.spawn(apipath);
	}
	catch (e)
	{
		throw "Could not spawn api from location: " + apipath;
	}
	
	if (mainWindow == null)
		createWindow();
	
	apiProcess.stderr.on('data', (data) =>
	{
		if(mainWindow != null)
			mainWindow.webContents.send("notify", "Backend error, click here for details.|" + data);
	});
	
	apiProcess.on("close", (code, signal) =>
	{
		if(mainWindow != null)
			mainWindow.webContents.send("notify", "Backend closed (" + code + ") (" + signal + "), you may need to restart.");
	});
}

// When we close our app the api should also close.
process.on('exit', function ()
{
	if(apiProcess != null)
		apiProcess.kill();
});

autoUpdater.on("update-not-available", (info) =>
{
	if(currentVersion != info.version)
		sendPreReleaseNotification(info.version);
	else
		mainWindow.webContents.send("notify",
			"Program is up to date!|@https://github.com/Naxesss/MapsetVerifier/releases/latest");
});

autoUpdater.on("update-available", (info) =>
{
	if(currentVersion != info.version && isPreRelease)
		sendPreReleaseNotification(info.version);
	else if (os.platform() === "darwin" || os.platform() == "linux" || isPreRelease)
		mainWindow.webContents.send("notify", "Update to " + info.version + " available.|@https://github.com/Naxesss/MapsetVerifier/releases/latest");
	else
		mainWindow.webContents.send("notify", "Update to " + info.version + " available, downloading...");
});

function sendPreReleaseNotification(latestVersion)
{
	mainWindow.webContents.send("notify",
			"Program is running version " + currentVersion + "." +
			"<br \>Latest release is " + latestVersion + ".|@https://github.com/Naxesss/MapsetVerifier/releases/latest");
}

autoUpdater.on('download-progress', (progressObj) =>
{
	mainWindow.setProgressBar(progressObj.transferred / progressObj.total);
});

autoUpdater.on("update-downloaded", () =>
{
	mainWindow.webContents.send("notify", "Update downloaded, restarting...");
	
	// Give the user time to see the above.
	setTimeout(() =>
	{
		autoUpdater.quitAndInstall(false, true);
	}, 3000)
});

autoUpdater.on("error", (error) =>
{
	mainWindow.webContents.send("notify", "Update failed, click here for details.|" + error +
	"<br><br>You may need to download it manually. https://github.com/Naxesss/MapsetVerifier/releases/latest");
});

function checkForUpdates()
{
	if (os.platform() === "darwin" || os.platform() == "linux" || isPreRelease)
		autoUpdater.autoDownload = false;
	
	autoUpdater.checkForUpdates();
}