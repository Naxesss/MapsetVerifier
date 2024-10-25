var spawnTimeout;
var isSpawningLink = false;
var tooltipExists = false;

/// Returns the HTML structure of a tooltip.
function getTooltip(tooltip)
{
	return "<div class=\"tooltip\" data-tip=\"" + tooltip + "\"><div class=\"tooltip-arrow\"></div></div>";
}

/// Creates a tooltip object at the element position.
function spawnTooltip(element, isLink = false)
{
	var tooltipData = "";
	if(!isLink)
		tooltipData = $(element).data("tooltip");
	else
		tooltipData = $(element).attr("href");
	
	var tooltip = getTooltip(tooltipData);
	var tooltipElement = $(tooltip).appendTo($("#wrapper"));
	var tooltipArrow = $(tooltipElement).children(".tooltip-arrow");
	
	var newTop = $(element).offset().top + $(element).height() - 10;
	var newLeft = $(element).offset().left + $(element).outerWidth() / 2 - $(tooltipElement).width() / 2;
	
	var offsetX = 0;
	var newRight = newLeft + $(tooltipElement).width();
	
	var borderLeft = 24;
	var borderRight = $(window).width() - 24;
	
	if(newLeft  < borderLeft)  offsetX = borderLeft  - newLeft;
	if(newRight > borderRight) offsetX = borderRight - newRight;
	
	var padding = 4;
	
	$(tooltipElement).css({ "top": newTop, "left": newLeft + offsetX - padding });
	$(tooltipArrow)  .css({ "top": 0, "left": $(tooltipElement).width() / 2 - offsetX + padding});
	
	$(tooltipElement).hide();
	tooltipExists = true;
	isSpawningLink = isLink;
	spawnTimeout = setTimeout(() =>
	{
		if(isLink)
			$(tooltipElement).addClass("link");
		$(tooltipElement).fadeIn(100).show();
	}, 200);
}

/// Removes all tooltip objects.
function despawnTooltip()
{
	var tooltipElement = $(".tooltip");
	
	if(tooltipElement)
	{
		$(tooltipElement).stop(true).fadeOut(100);
		setTimeout(function()
		{
			$(tooltipElement).remove();
		}, 100);
	}
}

/// Initializes the events necessary to spawn and despawn tooltips.
(function()
{
	$("body").mousemove(function(e)
	{
		// If the user moves their mouse, remove existing tooltips and clear spawn countdowns.
		if(tooltipExists)
			despawnTooltip();
		if(spawnTimeout && !isSpawningLink)
			clearTimeout(spawnTimeout);

		if($(e.target).closest(".no-tooltip").length == 0)
		{
			// If they moved it over an element, prepare to open a tooltip in case the cursor doesn't move again.
			if($(e.target).is("[data-tooltip]"))
				spawnTooltip($(e.target));
			else if($(e.target).parents("[data-tooltip]").length > 0)
				spawnTooltip($(e.target).parents("[data-tooltip]"));
			else if($(e.target).is("a[href!=\"\"]"))
				spawnTooltip($(e.target), true);
		}
	});
})();