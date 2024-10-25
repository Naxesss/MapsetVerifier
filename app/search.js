// Contains functions related to the beatmap listing (the left menu)

window.$ = window.jQuery = require('jquery');
const utils = require("./utils.js");
const Registry = require("winreg");

// HKEY_CLASSES_ROOT\\osu\\DefaultIcon usually contains the song folder path
var songFolderPath = "";

/// Whether or not something is being typed in the search bar
var searchInput = false;

const fs = require('fs');
const path = require('path');
const util = require('util');

// Turn readFile into a promise so that we can coordinate async events easier.
fs.readFileAsync = util.promisify(fs.readFile);

// Variables used in the LoadStep function
var iteration 		= 0;
var stepSize 		= 16;
var currentSearch 	= "";
var searchStatus 	= 0;

const appdataPath =
	process.env.APPDATA ||
	(process.platform == 'darwin' ?
		process.env.HOME + 'Library/Preferences' :
		process.env.HOME + "/.local/share");
const settingsPath = appdataPath + "/Mapset Verifier Externals/settings.ini";

var searchFieldTimer;

module.exports =
{
	/// Loads the path to the song folder from the settings
	loadSongFolder: function()
	{
		songFolderPath = utils.readFile(settingsPath, "song-folder-path");
		if(songFolderPath == "")
		{
			const html = "<div style=\"display: flex; justify-content: center;\"><div class=\"large-search-icon loading-icon\"></div></div>"
			var loadingDiv = $(html).appendTo("#left-content-mapsets");
			
			var regkey = new Registry(
			{
				hive: Registry.HKCR,
				key: "\\osu\\DefaultIcon"
			});
			
			regkey.values((err, items) =>
			{
				if(err != null)
				{
					console.log(err);
					return;
				}
				
				if(items.length > 0)
				{
					var parsedPath = items[0].value.substr(1, 12) + "Songs";
					
					fs.exists(parsedPath, exists =>
					{
						if(exists)
						{
							songFolderPath = parsedPath;
							utils.saveFile(settingsPath, "song-folder-path", songFolderPath);
							console.log("Found song folder in registry: \"" + parsedPath + "\".");
							
							loadingDiv.fadeOut(200);
							setTimeout(() =>
							{
								module.exports.reloadSearch();
							}, 200);
						}
						else
						{
							console.log("Registry contains an invalid song folder path: \"" + parsedPath + "\".");
							
							if($("#left-content-mapsets").find(".mapset-container").length == 0)
							{
								loadingDiv.fadeOut(200);
								setTimeout(() =>
								{
									$("#left-content-mapsets").append("<div id=\"left-content-mapsets-empty\">No song folder selected. Press the folder icon above the search bar.</div>");
								}, 200);
							}
						}
					});
				}
				else
				{
					console.log("Could not find song folder in registry.");
					loadingDiv.fadeOut(200);
					setTimeout(() =>
					{
						$("#left-content-mapsets").append("<div id=\"left-content-mapsets-empty\">No song folder selected. Press the folder icon above the search bar.</div>");
					}, 200);
				}
			});
		}
		else
			module.exports.reloadSearch();
	},

	/// Initializes the search bar and its functionality
	loadSearchBar: function()
	{
		$("input[data-pretext]").each(function()
		{
			$(this).val($(this).data("pretext"));
		}).focusin(function()
		{
			$(this).css("border-color", "rgba(255,255,255,1)");
			if(!$(this).data("pretextSet"))
			{
				$(this).val("");
				$(this).css("color", "rgba(255,255,255,1)");
			}
		}).focusout(function()
		{
			$(this).css("border-color", "rgba(255,255,255,0.4)");
			if($(this).val().length == 0)
			{
				$(this).val($(this).data("pretext"));
				$(this).css("color", "rgba(255,255,255,0.4)");
				
				$(this).data("pretextSet", false);
			}
			else
				$(this).data("pretextSet", true);
		});
		
		$("#left-content-search-bar").on("paste keyup", function()
		{
			// Prevents stuttering if the user is in the middle of typing.
			clearTimeout(searchFieldTimer);
			searchFieldTimer = setTimeout(() =>
			{
				searchInput = $("#left-content-search-bar").val().length > 0;
				module.exports.reloadSearch();
			}, 100);
		});
		
		$("#left-content-search-reload").click(() =>
		{
			module.exports.reloadSearch();
		});
		
		$("#left-content-search-folder").click(() =>
		{
			const songFolder = dialog.showOpenDialog({properties: ['openDirectory', 'createDirectory']});
			if(songFolder != null)
			{
				songFolderPath = songFolder;
				module.exports.reloadSearch();
				utils.saveFile(settingsPath, "song-folder-path", songFolder);
			}
		});
	},

	/// Refreshes the search results, resetting the amount displayed as well
	reloadSearch: function()
	{
		module.exports.clearSteps();
		module.exports.loadStep(searchInput ? $("#left-content-search-bar").val() : "");
	},

	/// Changes background and sends a post request for a check
	loadBeatmapSet: function(fullPath, bg)
	{
		document.dispatchEvent(new CustomEvent("Load BeatmapSet",
		{
			detail:
			{
				fullPath: fullPath
			}
		}));
	},
	
	/// Finds and loads beatmapset entries and the functionality of those entries
	loadStep: function(search)
	{
		if(songFolderPath == "")
		{
			$("#left-content-mapsets").before("<div id=\"left-content-mapsets-empty\">No song folder selected. Press the folder icon above the search bar. Select /osu!/Songs.</div>");
			return;
		}
		
		$("#left-content-mapsets-empty").remove();
		$("#left-content-mapsets").before(
			"<div id=\"left-content-mapsets-empty\">" +
			"<div style=\"display: flex; justify-content: center;\">" +
			"<div class=\"large-search-icon loading-icon\"></div></div></div>");
		
		// Opens the Songs folder and searches through beatmapset folders.
		(async function loop()
		{
			++searchStatus;
			
			const basedir = songFolderPath + path.sep;
			await utils.getDirectory(basedir, true).then(async folders =>
			{
				var found = 0;
				currentSearch = search;
				
				// Run this asynchronously so we can await each promise and that way get things in order.
				for(let index = iteration; index < folders.length; ++index)
				{
					let folder = folders[index];
					
					// Ensure that no more than a certain amount are loaded at once to prevent lag.
					if(found >= stepSize)
						break;
					
					// If the user changes the search, we want to abort what we're doing and not add divs of previous searches.
					if(currentSearch != search)
						break;
					
					// Should the folder somehow be undefined (which apparently happens), or the same as the previous, we ignore it.
					if(!folder)
						continue;
					
					// Keeps track of where we should return to when we want to continue the list.
					++iteration;
					
					// Opens the beatmapset folder and searches through the files.
					await utils.getDirectory(path.join(basedir, folder)).then(files =>
					{
						// In .some(), return true = break, return false = continue;
						files.some(file =>
						{
							(async function innerLoop()
							{
								// If none of the files end with .osu, it's not a beatmapset folder.
								if(file.endsWith(".osu"))
								{
									if(currentSearch != search)
										return true;
									
									// Open the .osu file to parse it's contents.
									await fs.readFileAsync(path.join(basedir, folder, file), 'utf8', (err, data) =>
									{
										// If the user changes the search, we want to abort what we're doing and not add divs of previous searches.
										if(currentSearch != search)
											return true;
										
										if (err)
											throw err;
										
										const bgFile = encodeURI(utils.scrape(data, "0,0,\"", "\""));
										const bgPath = path.join(basedir, folder).substring(basedir.startsWith("./") ? 2 : 0) + path.sep + bgFile;
										
										const title 			= utils.scrape(data, "Title:", "\n");
										const artist 			= utils.scrape(data, "Artist:", "\n");
										const creator 			= utils.scrape(data, "Creator:", "\n");
										const beatmapID 		= utils.scrape(data, "BeatmapID:", "\n");
										const beatmapSetID 		= utils.scrape(data, "BeatmapSetID:", "\n");
										const backgroundPath 	= utils.escape(bgPath);
										
										var mapsetTitle = (title != "" && artist != "" ? artist + " - " + title : folder.substring(folder.indexOf(" "), folder.length));
										if(mapsetTitle.length > 50)
											mapsetTitle = mapsetTitle.substring(0, 47).trim() + "...";
										
										let searchableString = title + " - " + artist + " | " + creator + " (" + beatmapID + " " + beatmapSetID + ")";
										if(!searchInput || searchableString.toLowerCase().includes($("#left-content-search-bar").val().toLowerCase()))
										{
											var html = "<div class=\"mapset-container noselect\" data-folder=\"" + folder
												+ "\"><div class=\"mapset-bg\" style=\"background-image:url("
												+ "'" + backgroundPath + "'"
												+ ")\"></div><div class=\"mapset-text\"><div class=\"mapset-title\">"
												+ mapsetTitle
												+ (creator != "" ? "<br><span style=\"font-size:14px;\">Mapped by " + creator + "</span>" : "")
												+ "</div></div></div>";
											
											// Trim any duplicates (going to the same path) that might be found for whatever reason.
											if($("#left-content-mapsets .mapset-container[data-folder=\"" + folder + "\"]").length)
												return true;
											
											// Adds a div for the set under the search bar.
											$("#left-content-mapsets").append(html);
											
											// Allows each set card to post to the server when clicked.
											$(".mapset-container").last().click(function()
											{
												const fullPath = path.join(basedir, folder);
												module.exports.loadBeatmapSet(fullPath);
											});
											
											$(".mapset-container").last().hide().fadeIn(500);
											
											// Keep track of how many actual beatmapset folders we've found so we can stop after a certain amount.
											++found;
										}
										
										// We only need to find one .osu file to confirm that it's a beatmapset folder.
										// Needs to be in readFile since it otherwise does stuff asynchronously.
										return true;
									});
								}
							})();
						});
					});
				}
			});
			
			--searchStatus;
			
			if($("#left-content-mapsets").children(".mapset-container").length == 0)
			{
				if(searchStatus == 0)
				{
					$("#left-content-mapsets-empty").slideUp(500);
					setTimeout(() =>
					{
						$("#left-content-mapsets-empty").remove();
					}, 500);
					
					if(search)
						$("#left-content-mapsets").before("<div id=\"left-content-mapsets-empty\">The search yielded no results.</div>");
					else
						$("#left-content-mapsets").before("<div id=\"left-content-mapsets-empty\">No mapsets could be found in this folder. Make sure you've selected /osu!/Songs.</div>");
				}
			}
			else if(searchStatus == 0)
			{
				$("#left-content-mapsets-empty").slideUp(500);
				setTimeout(() =>
				{
					$("#left-content-mapsets-empty").remove();
				}, 500);
			}
		})();
	},

	/// Resets the beatmap listing variables and empties it
	clearSteps: function()
	{
		$("#left-content-mapsets").empty();
		iteration = 0;
		prevFolder = "";
	}
};

/// Allows more beatmaps to be loaded in if the user scrolls further
$("#left-content").scroll(function()
{
	if($("#left-content").scrollTop() == $("#left-content")[0].scrollHeight - $("#left-content").height())
		module.exports.loadStep(searchInput ? $("#left-content-search-bar").val() : "");
});