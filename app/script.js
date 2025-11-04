const {dialog} 		= require('electron').remote;
const {ipcRenderer} = require('electron');
var Chart = require('chart.js')

window.$ = window.jQuery = require('jquery');
const search = require('./search.js');
const timelines = require('./timeline.js');
const server = require('./signalr.js');

const fs = require('fs');
const path = require('path');


var disabledIcons = ["minor", "check"];
var baseIcons 	  = ["check", "gear-gray"];

const disabledClass = "disabled";
const ignoredClass  = "ignored";

// Disabled reasons (gets enabled when all reasons to be disabled are gone)
const noDetailVisibleClass 	 = "no-detail-visible";
const notExpandedClass 		 = "not-expanded";
const hideIconClass 		 = "hide-icon";
const interpretMismatchClass = "interpret-mismatch";

const disabledReasons = [noDetailVisibleClass, notExpandedClass, hideIconClass, interpretMismatchClass];
const ignoredReasons  = [noDetailVisibleClass, hideIconClass, interpretMismatchClass];


/** INITIALIZATION **/

// Runs whenever the DOM is ready
$(() =>
{
	InitializeDragDrop();

	loadOptions();
	loadSettings();
	loadDocumentation();
	loadVersion();
	search.loadSearchBar();
	search.loadSongFolder();
	
	var hideListing = utils.readFile(settingsPath, "hideListing");
	if(hideListing != "1")
		toggleLeft();
	else
		$("#option-toggle-left").addClass("option-toggled");
	
	updateOptionBoxes();
	switchCategory("Checks", true);
	
	setTimeout(() =>
	{
		server.requestDocumentation();
		startLoadingTab("Documentation");
	}, 100);
	
	$(window).resize(function()
	{
		var currentCategory = $(".content-paste:visible").data("category");
		if(currentCategory == "documentation")
			fixDocBoxes();
	});
});

function loadVersion()
{
	var packageJson = require(__dirname + "/package.json");
	$("#top-logo-text").append(" " + packageJson.version);
}

function loadSettings()
{
	// General
	var noDefaultBg = utils.readFile(settingsPath, "noDefaultBg");
	if(noDefaultBg == "1")
	{
		toggleDefaultBg($("#option-toggle-default-bg")[0]);
		$("#option-toggle-default-bg").addClass("option-toggled");
	}
	
	var noBeatmapBg = utils.readFile(settingsPath, "noBeatmapBg");
	if(noBeatmapBg == "1")
	{
		toggleBeatmapBg($("#option-toggle-beatmap-bg")[0]);
		$("#option-toggle-beatmap-bg").addClass("option-toggled");
	}
	
	var noFlashing = utils.readFile(settingsPath, "noFlashing");
	if(noFlashing == "1")
		$("#option-disable-flashing").addClass("option-toggled");
	
	// Checks
	var showChecks = utils.readFile(settingsPath, "showChecks");
	if(showChecks == "1")
		$("#option-show-checks").addClass("option-toggled");
	
	var showMinor = utils.readFile(settingsPath, "showMinor");
	if(showMinor == "1")
		$("#option-show-minor").addClass("option-toggled");
	
	var contractCategories = utils.readFile(settingsPath, "contractCategories");
	if(contractCategories == "1")
		$("#option-contract-categories").addClass("option-toggled");

	// Overview
	var hideTimelineHs = utils.readFile(settingsPath, "hideTimelineHs");
	if (hideTimelineHs == "1")
	{
		let stylesheet = document.querySelector("link[href*=style]").sheet
		stylesheet.insertRule(".overview-timeline-hs { display: none; }", stylesheet.cssRules.length - 1);
	}

	var showNA = utils.readFile(settingsPath, "showNA");
	if(showNA == "1")
		$("#option-show-NA").addClass("option-toggled");
}

function loadOptions()
{
	$("#right-options-overflow").click(function()
	{
		toggleRight();
	});
	
	$("#option-toggle-left").click(function()
	{
		toggleLeft();
	});
	
	$("#option-toggle-default-bg").click(function()
	{
		toggleDefaultBg(this);
	});
	
	$("#option-toggle-beatmap-bg").click(function()
	{
		toggleBeatmapBg(this);
	});
	
	$("#option-disable-flashing").click(function()
	{
		var toggled = !$(this).hasClass("option-toggled");
		utils.saveFile(settingsPath, "noFlashing", toggled ? "1" : "0");
	});
	
	$("#option-contract-categories").click(function()
	{
		var toggled = !$(this).hasClass("option-toggled");
		$(".card-box").each(function()
		{
			if(toggled)
			{
				if($(this).hasClass("expanded"))
					if(collapseCardBox($(this)))
						$(this).removeClass("expanded");
			}
			else if(expandCardBox($(this)))
				$(this).addClass("expanded");
		});
		
		utils.saveFile(settingsPath, "contractCategories", toggled ? "1" : "0");
	});
	
	$(".right-category-option").click(function()
	{
		switchCategory($(this).data("category"));
	});
	
	$("#option-show-checks").click(function()
	{
		var toggled = !$(this).hasClass("option-toggled");
		
		if(toggled)
		{
			if(disabledIcons.includes("check"))
				disabledIcons = disabledIcons.filter(value => value != "check");
		}
		else if(!disabledIcons.includes("check"))
			disabledIcons.push("check");
		
		updateIssueVisibility(['H']);
		
		utils.saveFile(settingsPath, "showChecks", toggled ? "1" : "0");
	});
	
	$("#option-show-minor").click(function()
	{
		var toggled = !$(this).hasClass("option-toggled");
		
		if(toggled)
		{
			if(disabledIcons.includes("minor"))
				disabledIcons = disabledIcons.filter(value => value != "minor");
		}
		else if(!disabledIcons.includes("minor"))
			disabledIcons.push("minor");
		
		updateIssueVisibility(['H']);
		
		utils.saveFile(settingsPath, "showMinor", toggled ? "1" : "0");
	});

	$("#option-hide-timeline-hs").click(function()
	{
		var toggled = !$(this).hasClass("option-toggled");
		
		let stylesheet = document.querySelector("link[href*=style]").sheet
		if (toggled)
			stylesheet.insertRule(".overview-timeline-hs { display: none; }", stylesheet.cssRules.length - 1);
		else
			stylesheet.insertRule(".overview-timeline-hs { display: block; }", stylesheet.cssRules.length - 1);
		
		utils.saveFile(settingsPath, "hideTimelineHs", toggled ? "1" : "0");
	});

	$("#option-show-NA").click(function()
	{
		var toggled = !$(this).hasClass("option-toggled");
		
		$(".overview-field-content").each(function()
		{
			if($(this).text() == "N/A")
			{
				if(!toggled)
					$(this).parent().slideUp(200);
				else
					$(this).parent().slideDown(200);
			}
		});
		
		utils.saveFile(settingsPath, "showNA", toggled ? "1" : "0");
	});

	$("#option-shortcut-app").click(function()
	{
		shell.openItem(__dirname);
	});

	$("#option-shortcut-externals").click(function()
	{
		shell.openItem(appdataPath + "/Mapset Verifier Externals");
	});
	
	$(".option-box").click(function()
	{
		var toggled = $(this).hasClass("option-toggled");
		
		if(toggled)
			$(this).removeClass("option-toggled");
		else
			$(this).addClass("option-toggled");
		
		updateOptionBox(this);
	});
}

function loadDocumentation()
{
	$("body").on("click", ".doc-box-container", function()
	{
		if(!$(this).parent().hasClass("doc-mode-icons"))
		{
			var checkMessage = $(this).find(".doc-box-title").text();
			server.requestOverlay(checkMessage);
		}
	});
	
	$("#overlay").click(function(e)
	{
		if($(e.target).is("#overlay"))
		{
			$("#overlay").fadeOut(200);
			$("#overlay-outer").animate({marginTop:"0px"}, 200);
		}
	});

	$("body").on("click", ".shows-info", function()
	{
		showOverlay($(this).data("shown-info"))
	});
}

function loadContent(tab)
{
	var tabfilter = ".content-paste[data-category=\"" + tab.toLowerCase() + "\"] ";
	
	$(tabfilter + ".content-paste").each(function()
	{
		var category = $(this).data("category");
		selectDifficulty(category);
	});
	
	$(tabfilter + ".beatmap-options-folder").click(function()
	{
		var folderPath = utils.decodeHtml($(this).data("folder"));
		shell.openItem(folderPath);
	});
	
	$(tabfilter + ".beatmap-options-web").click(function()
	{
		var setid = $(this).data("setid");
		shell.openExternal("https://osu.ppy.sh/beatmapsets/" + setid);
	});
	
	$(tabfilter + ".beatmap-options-discussion").click(function()
	{
		var setid = $(this).data("setid");
		var beatmapid = $(".beatmap-difficulty-selected").first().data("beatmapId");
		shell.openExternal(
			"https://osu.ppy.sh/beatmapsets/" + setid + "/discussion/" +
			(beatmapid == "-" ? beatmapid + "/generalAll" : beatmapid));
	});
	
	$(tabfilter + ".beatmap-options-link").click(function()
	{
		var setid = $(this).data("setid");
		shell.openExternal("osu://dl/" + setid);
	});
	
	$(tabfilter + ".beatmap-author").click(function()
	{
		var name = $(this).text();
		shell.openExternal("https://osu.ppy.sh/users/" + name);
	});
	
	$(tabfilter + ".interpret").click(function()
	{
		$(this).parent().children(".interpret-selected").each(function()
		{
			$(this).removeClass("interpret-selected");
		});
		
		$(this).addClass("interpret-selected");
		
		var category 	= $(this).closest(".content-paste").data("category");
		
		applyConditions(category);
		
		if(category == "snapshots")
			selectDifficulty(category);
	});
	
	$(tabfilter + ".card-detail-toggle").click(function()
	{
		var details = $(this).parent().parent().next();
		if($(this).hasClass("vertical-arrow-toggled"))
		{
			$(this).removeClass("vertical-arrow-toggled");
			$(details).animate({height:"toggle", opacity:"toggle"}, 200).show(0);
		}
		else
		{
			$(this).addClass("vertical-arrow-toggled");
			$(details).animate({height:"toggle", opacity:"toggle"}, 200).hide(0);
		}
	});
	
	$(tabfilter + ".doc-shortcut").click(function()
	{
		var checkMessage = $(this).parent().data("check");
		server.requestOverlay(checkMessage);
	});
	
	$(tabfilter + ".card-box").click(function()
	{
		toggleCardBox($(this));
	});
	
	$(tabfilter + ".beatmap-difficulty").click(function()
	{
		var className = "beatmap-difficulty-selected";
		var category = $(this).parent().parent().data("category");
		if(!$(this).hasClass(className))
		{
			$(".content-paste[data-category=\"" + category + "\"] .beatmap-difficulty-selected").removeClass(className);
			$(this).addClass(className);
			
			selectDifficulty(category);
		}
	});
	
	applyConditions(tab, true);

	$(tabfilter + ".overview-field-title, " + tabfilter + ".overview-container-title").click(function()
	{
		var next = $(this).next();
		$(next).slideToggle(200);
	});

	$(tabfilter + ".overview-field-content").each(function()
	{
		if($(this).text() == "N/A")
		{
			$(this).css("color", "rgba(255,255,255,0.4)");
			if(!$("#option-show-NA").hasClass("option-toggled"))
				$(this).parent().hide();
		}
	});

	if(tab.toLowerCase() == "overview")
		timelines.init();

	$(tabfilter + ".overview-colour").each(function()
	{
		if($(this).data("colour") != "")
		{
			$(this).css("background-color", "rgb(" + $(this).data("colour") + ")")
		}
		else
		{
			$(this).css("border", "rgba(255,255,255,0.4) 1px solid");
			$(this).css("width", "16px");
			$(this).css("height", "16px");
		}
	});
	
	var toggledChecks = $("#option-show-checks").hasClass("option-toggled");
	if(toggledChecks && disabledIcons.includes("check"))
		disabledIcons = disabledIcons.filter(value => value != "check");
	
	var toggledMinor = $("#option-show-minor").hasClass("option-toggled");
	if(toggledMinor && disabledIcons.includes("minor"))
		disabledIcons = disabledIcons.filter(value => value != "minor");
	
	var contractCategories = $("#option-contract-categories").hasClass("option-toggled");
	$(tabfilter + ".card-box").each(function()
	{
		if(contractCategories)
		{
			if($(this).hasClass("expanded"))
				collapseCardBox($(this));
		}
		else if(expandCardBox($(this)))
			$(this).addClass("expanded");
	});
	
	if(!$("#option-disable-flashing").hasClass("option-toggled") && tab == "Checks")
	{
		var win = remote.getCurrentWindow();
		win.once('focus', () => win.flashFrame(false))
		if(!win.isFocused())
			win.flashFrame(true);
	}
	
	setTimeout(() =>
	{
		updateIssueVisibility(['H'], true, tab);
		slideUpCategory(tab);
		updateSelectSeparator();
		selectDifficulty(tab.toLowerCase());
	}, 600);
}

function InitializeDragDrop()
{
	$("body")
	.on('dragover', false)
	.on("drop", (e) =>
	{
		e.preventDefault();
		e.stopPropagation();

		if(e.originalEvent.dataTransfer.items)
		{
			for (let i = 0, item; item = e.originalEvent.dataTransfer.items[i]; i++)
				if (item.kind == "file")
					search.loadBeatmapSet(item.getAsFile().path);
		}
		else
			for (let i = 0, file; file = e.originalEvent.dataTransfer.files[i]; i++)
				search.loadBeatmapSet(file.path);
	});
}


/** LOADING **/

function startLoadingTab(tab)
{
	$(".content-paste[data-category='" + tab.toLowerCase() + "']").empty();
	$(".main-content[data-category='" + tab.toLowerCase() + "'] .main-content-loading").addClass("loading");
}

function isLoadingTab(tab)
{
	return $(".main-content[data-category='" + tab.toLowerCase() + "'] .main-content-loading").hasClass("loading");
}

function stopLoadingTab(tab)
{
	var query = ".main-content[data-category='" + tab.toLowerCase() + "'] ";
	
	if($(query + ".main-content-loading").hasClass("loading"))
	{
		if($(query + ".main-content-loading").is(":visible"))
		{
			// Apparently doing a timeout works on linux but a .once("animationend", ...) does not.
			$(query + ".main-content-loading").addClass("loading-fade");
			setTimeout(() =>
			{
				$(query + ".main-content-loading").removeClass("loading");
				$(query + ".main-content-loading").removeClass("loading-fade");
				$(query + ".load-info-container").empty();
			}, 400);
		}
		else
		{
			$(query + ".main-content-loading").removeClass("loading");
			$(query + ".main-content-loading").removeClass("loading-fade");
			$(query + ".load-info-container").empty();
		}
	}
}

function addLoad(loadMessage, tab)
{
	var mainQuery = ".main-content[data-category=\"" + tab.toLowerCase() + "\"] ";
	var html = "<div class=\"load-info\">" + loadMessage + "</div>";
	
	var loadInstance = $(html).appendTo(mainQuery + ".load-info-container");
	
	$(loadInstance).addClass("animation-enable-detail");
	$(loadInstance).removeClass("animation-disable-detail");
	$(loadInstance).css("display", "block");
	setTimeout(() =>
	{
		$(loadInstance).removeClass("animation-enable-detail");
	}, 200);
}

function removeLoad(loadMessage, tab)
{
	var mainQuery = ".main-content[data-category=\"" + tab.toLowerCase() + "\"] ";
	var loadInstance = $(".main-content[data-category=\"" + tab.toLowerCase() + "\"]").find(".load-info:contains(" + utils.decodeHtml(loadMessage) + ")");
	
	$(loadInstance).addClass("animation-disable-detail");
	$(loadInstance).removeClass("animation-enable-detail");
	setTimeout(() =>
	{
		if($(loadInstance).hasClass("animation-disable-detail"))
			$(loadInstance).remove();
	}, 200);
}

function clearLoad(tab)
{
	var mainQuery = ".main-content[data-category=\"" + tab.toLowerCase() + "\"] ";
	$(mainQuery + ".load-info").each(function()
	{
		$(this).remove();
	});
}


/** LAYOUT **/

function toggleLeft()
{
	var toggled = $("#left").hasClass("left-toggled");
	if(!toggled)
	{
		// show
		$("#left").addClass("left-toggled");
		$("#main").addClass("left-toggled");
	}
	else
	{
		// hide
		$("#left").removeClass("left-toggled");
		$("#main").removeClass("left-toggled");
	}
	
	utils.saveFile(settingsPath, "hideListing", toggled ? "1" : "0");
}

function toggleRight()
{
	var toggled = $("#right").hasClass("right-toggled");
	if(!toggled)
	{
		// show
		$("#right").addClass("right-toggled");
		$("#main").addClass("right-toggled");
	}
	else
	{
		// hide
		$("#right").removeClass("right-toggled");
		$("#main").removeClass("right-toggled");
	}
}

var count = 0;
function selectDifficulty(category)
{
	var selectedDifficulties = $(".content-paste[data-category=\"" + category + "\"] .beatmap-difficulty-selected");
	if(selectedDifficulties.length > 0)
	{
		var selectedDifficulty = selectedDifficulties.first().data("difficulty");
		
		// hide those that aren't in this diff
		$.each($(".content-paste[data-category=\"" + category + "\"] .interpret-container"), function()
		{
			if($(this).parent().parent().data("difficulty") != selectedDifficulty && category != "snapshots" && $(this).css("display") != "none")
				$(this).stop(true, true).animate({height:"toggle", opacity:"toggle"}, 200).hide(0);
		});
		
		// and then show those that are
		count = 0;
		$.each($(".content-paste[data-category=\"" + category + "\"] .interpret-container"), function()
		{
			if($(this).parent().parent().data("difficulty") == selectedDifficulty || category == "snapshots")
			{
				if(!$(this).is(":visible"))
				{
					$(this).stop(true, true).delay(count * 25).animate({height:"toggle", opacity:"toggle"}, 200).show(0);
					if(!$(this).hasClass("disabled"))
						count++;
				}
			}
		});
	}
	
	updateIssueVisibility();
}

function toggleDefaultBg(box)
{
	var toggled = !$(box).hasClass("option-toggled");
	
	if(toggled)
	{
		$("#front-default-bg").fadeOut(500);
		setTimeout(() =>
		{
			$("#front-default-bg").addClass("disabled");
		}, 500);
	}
	else
	{
		$("#front-default-bg").removeClass("disabled");
		
		if($("#front-beatmap-bg").css("background-image") == "none" || $("#option-toggle-beatmap-bg").hasClass("option-toggled"))
			$("#front-default-bg").hide(0).fadeIn(500);
	}
	
	utils.saveFile(settingsPath, "noDefaultBg", toggled ? "1" : "0");
}

function toggleBeatmapBg(box)
{
	var toggled = !$(box).hasClass("option-toggled");
	var hasBg = $("#front-beatmap-bg").css("background-image") != "none";
	
	if(toggled)
	{
		if(!$("#option-toggle-default-bg").hasClass("option-toggled") && hasBg)
		{
			$("#front-default-bg").removeClass("disabled");
			$("#front-default-bg").hide(0).fadeIn(500);
		}
		
		$("#front-beatmap-bg").fadeOut(500);
		setTimeout(() =>
		{
			$("#front-beatmap-bg").addClass("disabled");
		}, 500);
	}
	else
	{
		if(hasBg)
			$("#front-default-bg").fadeOut(500);
		
		$("#front-beatmap-bg").removeClass("disabled");
		$("#front-beatmap-bg").hide(0).fadeIn(500);
	}
	
	utils.saveFile(settingsPath, "noBeatmapBg", toggled ? "1" : "0");
}

function updateOptionBox(box)
{
	var optionTick = $(box).children().first();
	var toggled = $(box).hasClass("option-toggled");
	
	if(!toggled)
		optionTick.stop(true, true).animate({marginLeft:"3px"}, 200);
	else
		optionTick.stop(true, true).animate({marginLeft:"19px"}, 200);
}

function updateOptionBoxes()
{
	$(".option-box").each(function()
	{
		updateOptionBox(this)
	});
}

function showOverlay(content)
{
	$("#overlay-inner").empty();
	$("#overlay-inner").html(content);
	
	$("#overlay").fadeIn(200);
	$("#overlay-outer").css({marginTop:"0px"});
	$("#overlay-outer").animate({marginTop:"32px"}, 200);
}

function switchCategory(category, fallIn = false)
{
	var toggleClass = "right-option-toggled";
	
	$(".right-category-option").removeClass(toggleClass)
	$(".right-category-option[data-category='" + category + "']").addClass(toggleClass);
	
	$(".content-paste").each(function()
	{
		if($(this).data("category") != category.toLowerCase())
		{
			$(this).parent().addClass("transition-fast");
			$(this).parent().css({"transform": "translateX(64px)", "opacity": "0"});
			setTimeout(() =>
			{
				$(this).parent().hide();
				$(this).parent().removeClass("transition-fast");
			}, 200)
		}
	});
	
	if(fallIn)
		slideUpCategory(category);
	else
		slideSidewaysCategory(category);
	
	updateSelectSeparator();
}

function slideSidewaysCategory(category)
{
	setTimeout(() =>
	{
		$(".content-paste[data-category='" + category.toLowerCase() + "']").each(function()
		{
			if(!$(this).parent().is(":visible") || $(this).parent().hasClass("transition-fast"))
			{
				$(this).parent().addClass("transition-instant");
				$(this).parent().hide().css({"opacity": "0", "transform": "translate(64px, 0px)"});
				$(this).parent().removeClass("transition-instant");
				
				var toggled = $(".right-option-toggled[data-category='" + category + "']").length > 0;
				if(toggled)
				{
					$(this).parent().addClass("transition-fast");
					$(this).parent().css({"display": "flex"});
					setTimeout(() =>
					{
						$(this).parent().css({"opacity": "1", "transform": "translate(0px, 0px)"});
						if(category.toLowerCase() == "documentation")
							fixDocBoxes();
					}, 50);
					setTimeout(() =>
					{
						$(this).parent().removeClass("transition-fast");
					}, 200);
				}
			}
		});
	}, 200);
}

function slideUpCategory(category)
{
	$(".content-paste[data-category=\"" + category.toLowerCase() + "\"]").each(function()
	{
		$(this).parent().addClass("transition-instant");
		$(this).parent().hide().css({"opacity": "0", "transform": "translate(0px, 100%)"});
		setTimeout(() =>
		{
			$(this).parent().removeClass("transition-instant");
		}, 0);
		
		var toggled = $(".right-option-toggled[data-category='" + category + "']").length > 0;
		if(toggled)
			$(this).parent().css("display", "flex");
		
		setTimeout(() =>
		{
			$(this).css("display", "block");
			$(this).parent().css({"opacity": "1", "transform": "translate(0px, 0px)"});
			if(category.toLowerCase() == "documentation")
				fixDocBoxes();
		}, 50);
	});
}

function fixDocBoxes()
{
	$(".doc-mode-inner").each(function()
	{
		var lastbox = $(this).children().last();
		var firstbox = $(this).children().first();
		
		if(lastbox.is(firstbox))
			return;
		
		lastbox.css({"width": firstbox.outerWidth(), "flex": "none"});
	});
}

function updateCategory(category, html)
{
	$(".content-paste[data-category='" + category.toLowerCase() + "']").css("display", "none");
	$(".content-paste[data-category='" + category.toLowerCase() + "']").html(html);
}


/** DETAILS **/

function updateArrows()
{
	$.each($(".vertical-arrow"), function()
	{
		var next = $(this).parent().parent().next();
		if($(next).children("." + disabledClass).length + $(next).children(".animation-disable-detail").length == $(next).children(".card-detail").length)
			$(this).hide(0);
		else
			$(this).show(0);
	});
}

var lazyInterpretContainer = "";
var lazyInterpretCategory = "";
var lazyInterpretDiffName = "";
var lazyInterpretConditionType = "";

function getInterpretSeverity(category, difficultyName, dataType)
{
	if(category == lazyInterpretCategory && difficultyName == lazyInterpretDiffName && dataType == lazyInterpretConditionType)
		return lazyInterpretContainer.find(".interpret-selected").eq(0).data("interpretSeverity");
	
	var base = $(".content-paste[data-category=\"" + category + "\"]").eq(0);
	if(category != "snapshots")
		base = base.find(".card-difficulty[data-difficulty=\"" + difficultyName + "\"]").eq(0);
	
	// Speeds things up if the next call to this uses the same arguments.
	lazyInterpretContainer 		= base.find(".interpret-container[data-interpret=\"" + dataType + "\"]").eq(0);
	lazyInterpretCategory 		= category;
	lazyInterpretDiffName 		= difficultyName;
	lazyInterpretConditionType 	= dataType;
	
	return lazyInterpretContainer.find(".interpret-selected").eq(0).data("interpretSeverity");
}

function applyConditions(tab, initial = false)
{
	updateIssueVisibility(['I', 'H'], initial, tab);
}

function updateIcons(diff, atype)
{
	var wasChanged = false;
	
	$(".content-paste[data-category=\"checks\"] .card-details > .card-detail .card-detail-icon").each(function()
	{
		if($(this).parent().next()[0] != null && $(this).parent().next()[0].className.includes("card-detail-instances"))
		{
			var instances = $(this).parent().next();
			
			if(   $(instances).find(".card-detail > .cross-icon").length >
				  $(instances).find(".card-detail." + ignoredClass + " > .cross-icon").length)
			{
				if(changeIcon(this, "cross-icon"))
					wasChanged = true;
			}
			else if($(instances).find(".card-detail > .exclamation-icon").length >
					$(instances).find(".card-detail." + ignoredClass + " > .exclamation-icon").length)
			{
				if(changeIcon(this, "exclamation-icon"))
					wasChanged = true;
			}
			else if($(instances).find(".card-detail > .minor-icon").length >
					$(instances).find(".card-detail." + ignoredClass + " > .minor-icon").length)
			{
				if(changeIcon(this, "minor-icon"))
					wasChanged = true;
			}
			else if($(instances).find(".card-detail > .error-icon").length >
					$(instances).find(".card-detail." + ignoredClass + " > .error-icon").length)
			{
				if(changeIcon(this, "error-icon"))
					wasChanged = true;
			}
			else
			{
				if(changeIcon(this, "check-icon"))
					wasChanged = true;
			}
		}
		else
			if(changeIcon(this, "check-icon"))
				wasChanged = true;
	});
	
	$(".content-paste[data-category=\"checks\"] .card-box > .large-icon").each(function()
	{
		var cardDetailsContainer = $(this).parent().next();
		
		if(   $(cardDetailsContainer).find(".card-details > .card-detail .cross-icon").length >
			  $(cardDetailsContainer).find(".card-details > .card-detail." + ignoredClass + " .cross-icon").length)
		{
			if(changeIcon(this, "cross-icon"))
				wasChanged = true;
		}
		else if(   $(cardDetailsContainer).find(".card-details > .card-detail .exclamation-icon").length >
			       $(cardDetailsContainer).find(".card-details > .card-detail." + ignoredClass + " .exclamation-icon").length)
		{
			if(changeIcon(this, "exclamation-icon"))
				wasChanged = true;
		}
		else if(   $(cardDetailsContainer).find(".card-details > .card-detail .minor-icon").length >
				   $(cardDetailsContainer).find(".card-details > .card-detail." + ignoredClass + " .minor-icon").length)
		{
			if(changeIcon(this, "minor-icon"))
				wasChanged = true;
		}
		else if(   $(cardDetailsContainer).find(".card-details > .card-detail .error-icon").length >
				   $(cardDetailsContainer).find(".card-details > .card-detail." + ignoredClass + " .error-icon").length)
		{
			if(changeIcon(this, "error-icon"))
				wasChanged = true;
		}
		else
		{
			if(changeIcon(this, "check-icon"))
				wasChanged = true;
		}
	});
	
	$(".content-paste[data-category=\"checks\"] .beatmap-difficulty > .medium-icon").each(function()
	{
		var diff = $(this).parent().data("difficulty");
		if($(".content-paste[data-category=\"checks\"] .card[data-difficulty=\"" + diff + "\"] .card-box .cross-icon").length > 0)
		{
			if(changeIcon(this, "cross-icon"))
				wasChanged = true;
		}
		else if($(".content-paste[data-category=\"checks\"] .card[data-difficulty=\"" + diff + "\"] .card-box .exclamation-icon").length > 0)
		{
			if(changeIcon(this, "exclamation-icon"))
				wasChanged = true;
		}
		else if($(".content-paste[data-category=\"checks\"] .card[data-difficulty=\"" + diff + "\"] .card-box .minor-icon").length > 0)
		{
			if(changeIcon(this, "minor-icon"))
				wasChanged = true;
		}
		else if($(".content-paste[data-category=\"checks\"] .card[data-difficulty=\"" + diff + "\"] .card-box .error-icon").length > 0)
		{
			if(changeIcon(this, "error-icon"))
				wasChanged = true;
		}
		else
		{
			if(changeIcon(this, "check-icon"))
				wasChanged = true;
		}
	});
	
	$(".content-paste[data-category=\"snapshots\"] .card-box > .large-icon").each(function()
	{
		var cardDetailsContainer = $(this).parent().next();
		
		var disabledCount = $(cardDetailsContainer).find(".card-details .card-detail." + ignoredClass + " .plus-icon").length
			+ $(cardDetailsContainer).find(".card-details .card-detail." + ignoredClass + " .gear-blue-icon").length
			+ $(cardDetailsContainer).find(".card-details .card-detail." + ignoredClass + " .minus-icon").length;
			
		var totalCount = $(cardDetailsContainer).find(".card-details .card-detail .plus-icon").length
			+ $(cardDetailsContainer).find(".card-details .card-detail .gear-blue-icon").length
			+ $(cardDetailsContainer).find(".card-details .card-detail .minus-icon").length
		
		if(totalCount > disabledCount)
		{
			if(changeIcon(this, "gear-blue-icon"))
				wasChanged = true;
			
			if($(this).parent().parent().hasClass("gear-hidden"))
				$(this).parent().parent().removeClass("gear-hidden");
		}
		else
		{
			if(changeIcon(this, "gear-gray-icon"))
				wasChanged = true;
			
			if(!$(this).parent().parent().hasClass("gear-hidden"))
				$(this).parent().parent().addClass("gear-hidden");
		}
	});
	
	$(".content-paste[data-category=\"snapshots\"] .beatmap-difficulty > .medium-icon").each(function()
	{
		var diff = $(this).parent().data("difficulty");
		if($(".content-paste[data-category=\"snapshots\"]").find(".card[data-difficulty=\"" + diff + "\"] .card-box .gear-blue-icon").length > 0)
		{
			if(changeIcon(this, "gear-blue-icon"))
				wasChanged = true;
		}
		else
		{
			if(changeIcon(this, "gear-gray-icon"))
				wasChanged = true;
		}
	});
	
	// Recursively hide icons so that if this changes an icon to one that should be hidden it'll become hidden.
	if(wasChanged)
		updateIssueVisibility(['H']);
}

function changeIcon(object, iconClass)
{
	if(!$(object).hasClass(iconClass))
	{
		$(object).removeClass("cross-icon");
		$(object).removeClass("exclamation-icon");
		$(object).removeClass("minor-icon");
		$(object).removeClass("error-icon");
		$(object).removeClass("info-icon");
		$(object).removeClass("check-icon");
		$(object).removeClass("gear-gray-icon");
		$(object).removeClass("minus-icon");
		$(object).removeClass("plus-icon");
		$(object).removeClass("gear-blue-icon");
		
		$(object).addClass(iconClass);
		
		return true;
	}
	
	return false;
}

function updateAllDetailText()
{
	$(".card-detail").each(function()
	{
		updateDetailText(this);
	});
}

function updateDetailText(object)
{
	if($(object).find(".card-detail-text").length > 0)
		changeDetailText($(object).find(".card-detail-text").first(), $(object).find(".card-detail-icon.check-icon").length == 0);
}

function changeDetailText(object, isProblem)
{
	var currentText = $(object).text();
	if(isProblem)
	{
		if(currentText.startsWith("No"))
		{
			currentText = currentText.substr(3);
			$(object).text(currentText[0].toUpperCase()
						 + currentText.substr(1, currentText.length - 1));
		}
	}
	else
	{
		if(!currentText.startsWith("No"))
		{
			// Only lowercase the first character of the original string if the next character isn't uppercase.
			// e.g. "Unsnapped hit objects" -> "No unsnapped hit objects", whereas "BMS used as source" -> "No BMS used as source"
			let firstChar = currentText[0];
			if(currentText[1] != currentText[1].toUpperCase())
				firstChar = currentText[0].toLowerCase();

			$(object).text("No " + firstChar + currentText.substr(1, currentText.length - 1));
		}
	}
}

function updateSelectSeparator()
{
	setTimeout(() =>
	{
		var category = $(".content-paste:visible").first().data("category");
		var separatorClass = ".content-paste:visible .select-separator";
		
		if($(".card-container-unselected .card:visible").length > $(".card-container-unselected .card." + disabledClass + ":visible").length)
		{
			if(!$(separatorClass).is(":visible"))
				$(separatorClass).slideDown(200);
		}
		else
			if($(separatorClass).is(":visible"))
				$(separatorClass).slideUp(200);
	}, 300);
}

var truncateTimer;
function truncateCardDetails()
{
	cardDetails = $(".card-details");
	
	$(".show-more").each(function()
	{
		if($(this).next().children(".card-detail").not(".disabled").length + $(this).children(".card-detail.animation-enable-detail").length == 0)
		{
			$(this).before($(this).next().html());
			$(this).remove();
			$(this).next().remove();
		}
	});
	
	$(cardDetails).find(".show-more").each(function()
	{
		$(this).before($(this).next().html());
		$(this).remove();
		$(this).next().remove();
	});
	
	var showMore;
	var sameTextCounter;
	$(cardDetails).children(".card-detail-instances").each(function()
	{
		sameTextCounter = 0;
		var lastTemplate = "";
		$(this).children(".card-detail").each(function()
		{
			if(!$(this).hasClass("disabled") && !$(this).hasClass("animation-disable-detail"))
			{
				let tempTemplate = $(this).data("template");
				if(tempTemplate == lastTemplate)
					++sameTextCounter;
				else
					sameTextCounter = 0;
				lastTemplate = tempTemplate;
				
				if(sameTextCounter >= 9)
				{
					if(sameTextCounter == 9)
					{
						$(this).prev().before("<div class=\"show-more\"></div><div class=\"show-more-box\"></div>");
						showMore = $(this).prev().prev().prev();
						$(showMore).on("click", function()
						{
							$(this).toggleClass("shown");
							$(this).next().toggleClass("shown");
						});
						
						$(this).prev().appendTo($(showMore).next());
					}
					$(this).appendTo($(showMore).next());
					$(showMore).attr("data-amount", (sameTextCounter - 7).toString());
				}
			}
		})
	});
}

function toggleCardBox(cardBox)
{
	if($(cardBox).hasClass("card-selected"))
		collapseCardBox(cardBox);
	else
		expandCardBox(cardBox);
}

function expandCardBox(cardBox)
{
	var card = $(cardBox).parent(".card").first();
	var cardDetails = $(card).children(".card-details-container").children().first();
	var category = $(cardBox).closest(".content-paste").data("category");
	
	if(!$(cardBox).hasClass("card-selected"))
	{
		// select
		if($(card).is(":visible"))
		{
			$(card).animate({width:"toggle", opacity:"toggle"}, 200, () =>
			{
				$(cardBox).addClass("card-selected");
				$(card).appendTo(".content-paste[data-category=\"" + category + "\"] .card-container-selected");
				$(card).animate({height:"toggle", opacity:"toggle"}, 200);
				cardDetails.fadeIn(200).css({display:"table-row"});
			});
		}
		else
		{
			$(cardBox).addClass("card-selected");
			$(card).appendTo(".content-paste[data-category=\"" + category + "\"] .card-container-selected");
			cardDetails.fadeIn(200).css({display:"table-row"});
		}
		
		clearTimeout(truncateTimer);
		truncateTimer = setTimeout(() =>
		{
			truncateCardDetails();
		}, 200);
		
		setTimeout(() =>
		{
			updateSelectSeparator();
		}, 200);
		return true;
	}
	
	return false;
}

function collapseCardBox(cardBox)
{
	var card = $(cardBox).parent(".card").first();
	var cardDetails = $(card).children(".card-details-container").children().first();
	var category = $(cardBox).closest(".content-paste").data("category");
	
	if($(cardBox).hasClass("card-selected"))
	{
		// send back to the respective card container
		var appendDiv = $(".content-paste[data-category=\"" + category + "\"] .card-difficulty-checks").filter((index, value) => 
		{
			return $(value).parent().data("difficulty") == $(card).data("difficulty");
		}).first();
		
		// unselect
		cardDetails.fadeOut(200);
		if($(card).is(":visible"))
		{
			$(card).animate({height:"toggle", opacity:"toggle"}, 200, () =>
			{
				$(cardBox).removeClass("card-selected");
				$(card).appendTo(appendDiv);
				$(card).animate({width:"toggle", opacity:"toggle"}, 200);
			});
		}
		else
		{
			$(cardBox).removeClass("card-selected");
			$(card).appendTo(appendDiv);
		}
		
		updateSelectSeparator();
		return true;
	}
	
	return false;
}


/** SERVER CLIENT COMMUNICATION **/

function displayTab(tab, html)
{
	stopLoadingTab(tab);
	setTimeout(() =>
	{
		updateCategory(tab, html);
		loadContent(tab);
	}, 0);
}

var serverMessageHandler = (key, value) =>
{
	switch(key)
	{
		case "UpdateDocumentation":
			stopLoadingTab("Documentation");
			updateCategory("Documentation", value);
			slideUpCategory("Documentation");
			break;
		case "UpdateOverlay":
			showOverlay(value);
			break;
		case "UpdateChecks":
			displayTab("Checks", value);
			break;
		case "UpdateSnapshots":
			displayTab("Snapshots", value);
			break;
		case "UpdateOverview":
			displayTab("Overview", value);
			break;
		case "UpdateException":
			var tab = value.substring(0, value.indexOf(":"));
			var ex = value.substring(value.indexOf(":") + 1);
			displayTab(tab, ex);
			break;
		case "AddLoad":
			var tab = value.substring(0, value.indexOf(":"));
			var load = value.substring(value.indexOf(":") + 1);
			addLoad(load, tab);
			break;
		case "RemoveLoad":
			var tab = value.substring(0, value.indexOf(":"));
			var load = value.substring(value.indexOf(":") + 1);
			removeLoad(load, tab);
			break;
		case "ClearLoad":
			clearLoad(value);
			break;
		default:
			console.log("Received unknown message key \"" + key + "\" with value \"" + value + "\"");
	}
}

document.addEventListener("SignalR Connected", e =>
{
	e.detail.connection.off("ServerMessage", serverMessageHandler);
	e.detail.connection.on("ServerMessage", serverMessageHandler);
});

document.addEventListener("Load BeatmapSet", function(e)
{
	switchBgFromPath(e.detail.fullPath);

	clearLoad("Overview");
	clearLoad("Snapshots");
	clearLoad("Checks");
	
	switchCategory("Checks", false);
	
	startLoadingTab("Overview");
	startLoadingTab("Snapshots");
	startLoadingTab("Checks");
	
	server.requestBeatmapset(e.detail.fullPath);
});

async function switchBgFromPath(fullPath)
{
	var backgroundPath = "";

	// Opens the beatmapset folder and searches through the files.
	await utils.getDirectory(fullPath).then(files =>
	{
		// In .some(), return true = break, return false = continue;
		files.some(file =>
		{
			(async function innerLoop()
			{
				if(backgroundPath)
					return true;

				// If none of the files end with .osu, it's not a beatmapset folder.
				if(file.endsWith(".osu"))
				{
					// Open the .osu file to parse it's contents.
					await fs.readFileAsync(path.join(fullPath, file), 'utf8', (err, data) =>
					{
						if(backgroundPath)
							return true;

						if (err)
							throw err;
						
						const bgFile = encodeURI(utils.scrape(data, "0,0,\"", "\""));
						const bgPath = fullPath + path.sep + bgFile;
						backgroundPath = utils.escape(bgPath);

						switchBg(backgroundPath);
						
						// We only need to find one .osu file to confirm that it's a beatmapset folder.
						// Needs to be in readFile since it otherwise does stuff asynchronously.
						return true;
					});
				}
			})();
		});

		if(backgroundPath)
			return true;
	});

	return false;
}

function switchBg(bg)
{
	if(!$("#option-toggle-beatmap-bg").hasClass("option-toggled"))
		$("#front-default-bg").fadeOut(500);
	
	$("#front-beatmap-bg").fadeOut(500);
	setTimeout(() =>
	{
		$("#front-beatmap-bg").css("background-image", "url(\"" + bg + "\")");
		$("#front-beatmap-bg").fadeIn(500);
	}, 500);
}

ipcRenderer.on("notify", (e, message) =>
{
	// Anything after '|' in the message will be displayed when the notification is clicked.
	var notificationMessage = message;
	var extraInfo 			= "";
	if(message.includes('|'))
	{
		notificationMessage = message.split('|')[0];
		extraInfo 			= message.split('|')[1];
	}
	
	var html =
		"<div class=\"notification\"" +
		(extraInfo != "" ? " data-info" : "") +
		(extraInfo.startsWith("@") ? " data-link data-tooltip=\"" + utils.escape(extraInfo.substring(1)) + "\"" : "") +
		">" + notificationMessage + "</div>";
	
	var notification = $(html).appendTo("#notify");
	
	if(extraInfo != "")
	{
		$(notification).on("click", () =>
		{
			// Any notification with '|@' will instead open a link.
			if(extraInfo.startsWith("@"))
				shell.openExternal(extraInfo.substring(1));
			else
				showOverlay(extraInfo);
		});
	}
	
	$(notification).addClass("animation-enable-detail");
	$(notification).removeClass("animation-disable-detail");
	$(notification).css("display", "flex");
	setTimeout(() =>
	{
		$(notification).removeClass("animation-enable-detail");
		setTimeout(() =>
		{
			$(notification).addClass("animation-disable-detail");
			$(notification).removeClass("animation-enable-detail");
			setTimeout(() =>
			{
				$(notification).hide(0);
			}, 200);
		}, 5000);
	}, 200);
});


/** CATEGORIES & DETAILS **/

function updateIssueVisibility(options = [], initial = false, tab = "")
{
	var tabfilter = "";
	if(tab.length > 0)
		tabfilter = ".content-paste[data-category=\"" + tab.toLowerCase() + "\"] ";
	
	// Hide the detail if either the interpretation does not match or there is no icon to fall back to (i.e. checks are hidden).
	$.when.apply($, $(tabfilter + ".card-detail").map(function(item)
	{
		return new Promise((resolve, reject) =>
		{
			if(options.includes('I') && $(this).data("condition"))
			{
				if(interpretMismatch($(this)))
					$(this).addClass(interpretMismatchClass);
				else
					$(this).removeClass(interpretMismatchClass);
			}
			
			if(options.includes('H'))
			{
				// Hide the detail if either the base icon is hidden and it uses it, or it's a sub-detail and the respective icon is hidden.
				var shouldHideIcon = false;
				
				for(let i = 0; i < baseIcons.length; ++i)
				{
					if(disabledIcons.includes(baseIcons[i]) && $(this).children(".card-detail-icon:first").hasClass(baseIcons[i] + "-icon"))
						shouldHideIcon = true;
				}
				
				if($(this).closest("card-detail-instances"))
					for(let i = 0; i < disabledIcons.length; ++i)
						if($(this).children(".card-detail-icon:first").hasClass(disabledIcons[i] + "-icon"))
							shouldHideIcon = true;
				
				if(shouldHideIcon)
					$(this).addClass(hideIconClass);
				else
					$(this).removeClass(hideIconClass);
			}
			
			resolve();
		});
	})).then(() =>
	{
		addIgnored(() =>
		{
			removeIgnored(() =>
			{
				// Hide the category if either no details are visible or the difficulty isn't selected.
				$.when.apply($, $(tabfilter + ".card").map(function(item)
				{
					return new Promise((resolve, reject) =>
					{
						if(noDetailVisible($(this)))
							$(this).addClass(noDetailVisibleClass);
						else
							$(this).removeClass(noDetailVisibleClass);
						
						if(notExpanded($(this)))
							$(this).addClass(notExpandedClass);
						else
							$(this).removeClass(notExpandedClass);
						
						resolve();
					});
				})).then(() =>
				{
					addIgnored(() =>
					{
						updateDisabled(() =>
						{
							removeIgnored(() =>
							{
								if(options.includes('H'))
								{
									setTimeout(() =>
									{
										updateIcons();
										updateArrows();
										updateSelectSeparator();
										updateAllDetailText();
									}, 0);
								}
							});
						}, initial);
					});
				});
			});
		});
	});
}

function updateDisabled(callback, initial = false)
{
	var selector = disabledReasons.map(Class => "." + Class).join(", ");
	$.when.apply($, $(selector).map(function(item)
	{
		return new Promise((resolve, reject) =>
		{
			if(!$(this).hasClass(disabledClass))
				disable($(this), initial);
			
			resolve();
		});
	})).then(() =>
	{
		$.when.apply($, $("." + disabledClass).map(function(item)
		{
			return new Promise((resolve, reject) =>
			{
				var hasReason = false;
				for(let i = 0; i < disabledReasons.length; ++i)
				{
					if($(this).hasClass(disabledReasons[i]))
					{
						hasReason = true;
						break;
					}
				}
				
				if(!hasReason)
					enable($(this));
				
				resolve();
			});
		})).then(() =>
		{
			callback();
		});
	});
}

function addIgnored(callback)
{
	var selector = ignoredReasons.map(Class => "." + Class).join(", ");
	$.when.apply($, $(selector).map(function(item)
	{
		return new Promise((resolve, reject) =>
		{
			if(!$(this).hasClass(ignoredClass))
				$(this).addClass(ignoredClass);
			
			resolve();
		});
	})).then(() =>
	{
		callback();
	});
}

function removeIgnored(callback)
{
	$.when.apply($, $("." + ignoredClass).map(function(item)
	{
		return new Promise((resolve, reject) =>
		{
			var hasReason = false;
			for(let i = 0; i < ignoredReasons.length; ++i)
			{
				if($(this).hasClass(ignoredReasons[i]))
				{
					hasReason = true;
					break;
				}
			}
			
			if(!hasReason)
				$(this).removeClass("ignored");
			
			resolve();
		});
	})).then(() =>
	{
		callback();
	});
}

function noDetailVisible(object)
{
	if($(object).hasClass("card"))
		return $(object).find(".card-detail").length == $(object).find(".card-detail." + ignoredClass).length;
	
	if($(object).hasClass("beatmap-difficulty"))
	{
		var cards = $(".card-container-selected .card[data-difficulty=\"" + $(object).data("difficulty") + "\"]");
		var cardDiff = $(".card-difficulty[data-difficulty=\"" + $(object).data("difficulty") + "\"]").first();
		
		var noDetailVisible =
			(cardDiff.find(".card-detail").length +
			cards.find(".card-detail").length) ==
			(cardDiff.find(".card-detail." + ignoredClass).length +
			cards.find(".card-detail." + ignoredClass).length);
		
		return noDetailVisible;
	}
}

function interpretMismatch(object)
{
	var conditionType   = null;
	var conditionLevels = null;
	var diffName 		= null;
	if($(object).data("condition") != null)
	{
		conditionType   = $(object).data("condition").split("=")[0];
		conditionLevels = $(object).data("condition").split("=")[1];
		diffName 		= $(object).closest(".card").data("difficulty");
	}
	
	var category = $(object).closest(".content-paste").data("category");
	var severity = getInterpretSeverity(category, diffName, conditionType);
	
	if(conditionType == null || conditionLevels.includes(severity))
		return false;
	
	return true;
}

function notExpanded(object)
{
	if($(object).hasClass("card-detail"))
		return !$(object).closest(".card").children(".card-box:first").hasClass("card-selected");
	
	if($(object).hasClass("card"))
	{
		var tab = $(object).closest(".content-paste").data("category");
		
		return !$(".content-paste[data-category=\"" + tab + "\"] .beatmap-difficulty[data-difficulty=\"" + $(object).data("difficulty") + "\"]:first").hasClass("beatmap-difficulty-selected");
	}
}

function disable(object, initial = false)
{
	var visible = initial ? false : $(object).is(":visible");
	
	if($(object).hasClass("beatmap-difficulty"))
	{
		$(object).animate({marginLeft:"toggle", marginRight:"toggle", width:"toggle", opacity:"toggle"}, 200, () =>
		{
			$(object).addClass("disabled");
		});
	}
	
	if($(object).hasClass("card"))
	{
		var animationTime = 200;
		if(!visible)
			animationTime = 0;
		
		if(!$(object).find(".card-box:first").hasClass("card-selected") && $(object).hasClass(ignoredClass))
		{
			$(object).animate({width:"toggle", opacity:"toggle"}, animationTime, () =>
			{
				$(object).addClass("disabled");
			});
		}
		else
		{
			$(object).animate({height:"toggle", opacity:"toggle"}, animationTime, () =>
			{
				$(object).addClass("disabled");
			});
		}
	}
	
	if($(object).hasClass("card-detail"))
	{
		if(visible)
		{
			$(object).addClass("animation-disable-detail");
			$(object).removeClass("animation-enable-detail");
			setTimeout(() =>
			{
				$(object).addClass("disabled");
				$(object).removeClass("animation-disable-detail");
			}, 200);
		}
		else
		{
			$(object).addClass("disabled");
			$(object).removeClass("animation-disable-detail");
		}
		
		clearTimeout(truncateTimer);
		truncateTimer = setTimeout(() =>
		{
			truncateCardDetails();
		}, 200);
	}
}

function enable(object)
{
	if($(object).hasClass("beatmap-difficulty"))
	{
		$(object).removeClass("disabled");
		$(object).animate({marginLeft:"toggle", marginRight:"toggle", width:"toggle", opacity:"toggle"}, 200).show(0);
	}
	
	if($(object).hasClass("card"))
	{
		$(object).removeClass("disabled");
		
		var animationTime = 200;
		if($(object).hasClass("." + notExpandedClass))
			animationTime = 0;
		
		if(!$(object).find(".card-box").first().hasClass("card-selected") && $(object).hasClass(ignoredClass))
			$(object).animate({width:"toggle", opacity:"toggle"}, animationTime).show(0);
		else
			$(object).animate({height:"toggle", opacity:"toggle"}, animationTime).show(0);
	}
	
	if($(object).hasClass("card-detail"))
	{
		$(object).removeClass("disabled");
		$(object).removeClass("animation-disable-detail");
		if($(object).is(":visible"))
		{
			$(object).addClass("animation-enable-detail");
			setTimeout(() =>
			{
				$(object).removeClass("disabled");
				$(object).removeClass("animation-enable-detail");
			}, 200);
		}
		
		clearTimeout(truncateTimer);
		truncateTimer = setTimeout(() =>
		{
			truncateCardDetails();
		}, 200);
	}
}

function renderLineChart(canvasId, data)
{
	new Chart(canvasId, {
		type: 'line',
		data: data,
		options: {
			responsive: true,
			maintainAspectRatio: true,
			elements: {
				line: {
					tension: 0.2
				},
				point: {
					radius: 0
				}
			},
			scales: {
				xAxes: [{
					type: "linear",
					position: "bottom"
				}]
			},
			tooltips: {
				intersect: false,
				mode: "index",
				callbacks: {
					title: function (item, data) {
                        return formatToTimestamp(Number(item[0].label));
					},
					label: function (item, data) {
						return Number(item.value).toFixed(2)
					}
				}
			}
		}
	});

    function formatToTimestamp(inputSeconds) {
        var totalSeconds = inputSeconds.toFixed(3);
        var millisNum = (totalSeconds % 1) * 1000;
        var millisString = ((millisNum < 100) ? "0" : "") + ((millisNum < 10) ? "0" : "") + millisNum.toFixed(0);
        var secondsNum = totalSeconds % 60;
        var secondsString = ((secondsNum < 10) ? "0" : "") + secondsNum.toFixed(0) + ":";
        var minutesNum = (totalSeconds / 60) % 60;
        var minutesString = ((minutesNum < 10) ? "0" : "") + minutesNum.toFixed(0) + ":";
        var hoursNum = totalSeconds / 3600;
        var hoursString = (hoursNum >= 1) ? (hoursNum.toFixed(0) + ":") : "";
        return hoursString + minutesString + secondsString + millisString;
    }
}