window.$ = window.jQuery = require('jquery');

var zoomResistance = 40;

var currentTimeline;
var isDown = false;
var startX;
var scrollLeft = 0;
var totalZoom = 1;
var timelineId = 0;

module.exports =
{
    init: function()
    {
        totalZoom = 1;
        timelineId = 0;
        $(".overview-timeline").each(function()
        {
            updateTimelineBars(this);
        });
    }
}

$("#main").on("click", ".overview-timeline-button",function()
{
    if($(this).hasClass("plus-icon"))
        performZoom(20);
    else if($(this).hasClass("minus-icon"))
        performZoom(-20);
});

$("#main").on("click", "[data-timestamp]", function(e)
{
    if(e.altKey)
    {
        let timestamp = $(this).data("timestamp");
        shell.openExternal("osu://edit/" + timestamp);
    }
});

$("#main").on("contextmenu", "[data-timestamp]", function(e)
{
    if(e.altKey)
    {
        let timestamp = $(this).data("timestamp");
        copyToClipboard(timestamp);
        
        if ($(".tooltip").length > 0)
            $(".tooltip").text("Copied!")
    }
});

function copyToClipboard(text)
{
    let copy = document.createElement("textarea");
    document.body.appendChild(copy);
    copy.textContent = text;
    copy.select();
    document.execCommand("copy");
    document.body.removeChild(copy);
}

function performZoom(pendingZoom)
{
    // Seperate writing and reading to avoid reflow performance issues.
    // All the reading goes here.
    var marginLefts = [];
    var widths = [];
    var newScrollLefts = [];
    $(".overview-timeline-tick, .overview-timeline-object").each(function()
    {
        marginLefts.push(parseFloat($(this).css("margin-left")));
    });
    $(".overview-timeline-path").each(function()
    {
        widths.push(parseFloat($(this).css("width")));
    });
    $(".overview-timeline").each(function()
    {
        if(pendingZoom > 0)
            newScrollLefts.push((this.scrollLeft + this.clientWidth / 2) * (1 + Math.abs(pendingZoom) / zoomResistance) - this.clientWidth / 2);
        else
            newScrollLefts.push((this.scrollLeft + this.clientWidth / 2) * 1 / (1 + Math.abs(pendingZoom) / zoomResistance) - this.clientWidth / 2);
    });

    // All the writing goes here.
    $(".overview-timeline-tick, .overview-timeline-object").each(function(index, value)
    {
        if(pendingZoom > 0)
            $(this).css("margin-left", marginLefts[index] * (1 + Math.abs(pendingZoom) / zoomResistance));
        else
            $(this).css("margin-left", marginLefts[index] * 1 / (1 + Math.abs(pendingZoom) / zoomResistance));
    });
    $(".overview-timeline-path").each(function(index, value)
    {
        if(pendingZoom > 0)
            $(this).css("width", widths[index] * (1 + Math.abs(pendingZoom) / zoomResistance));
        else
            $(this).css("width", widths[index] * 1 / (1 + Math.abs(pendingZoom) / zoomResistance));
    });
    $(".overview-timeline").each(function(index, value)
    {
        if(pendingZoom > 0)
            this.scrollLeft = newScrollLefts[index];
        else
            this.scrollLeft = newScrollLefts[index];
    });

    if(pendingZoom > 0)
        totalZoom *= (1 + Math.abs(pendingZoom) / zoomResistance);
    else
        totalZoom *= 1 / (1 + Math.abs(pendingZoom) / zoomResistance);
    
    $(".overview-timeline-buttons-zoomamount").text(Math.round(totalZoom * 100) / 100 + "×");
}

$("#main").on("mousedown", ".overview-timeline", function (e)
{
    currentTimeline = this;
    isDown = true;
    startX = e.pageX - this.offsetLeft;
});

$("#main").mouseup(function ()
{
    isDown = false;
});

var ticking = false;
$("#main").mousemove(function (e)
{
    if(!isDown)
        return;
    
    e.preventDefault();

    let walkSpeed = 4;
    if(e.shiftKey)
        walkSpeed *= 12;
    if(e.ctrlKey)
        walkSpeed *= 0.25;

    const x = e.pageX - currentTimeline.offsetLeft;
    const walk = (x - startX) * walkSpeed;

    // Allows for pressing shift/ctrl and changing speed without teleporting.
    startX = x;

    if(walk == 0)
        return;

    if($(currentTimeline).hasClass("locked"))
    {
        var walkDir = walk / Math.abs(walk);
        var walkLength = Math.abs(walk);

        var scrollLefts = [];
        $(".overview-timeline.locked").not(".disabled").each(function()
        {
            // walkDir > 0 : backwards
            // walkDir < 0 : forwards

            if(walkDir > 0 && walkLength > this.scrollLeft)
                walkLength = this.scrollLeft;
            
            if(walkDir < 0 && walkLength > this.scrollWidth - (this.scrollLeft + this.clientWidth))
                walkLength = this.scrollWidth - (this.scrollLeft + this.clientWidth);

            scrollLefts.push(this.scrollLeft);
        });
        $(".overview-timeline.locked").not(".disabled").each(function(index, value)
        {
            this.scrollLeft = scrollLefts[index] - walkLength * walkDir;
        });

        // Update the slider bars for each timeline in a flow-friendly way (read/write seperately).
        updateTimelineBars($(".overview-timeline.locked").not(".disabled"));

        scrollLeft = $(".overview-timeline.locked").not(".disabled").first().scrollLeft();
    }
    else
    {
        currentTimeline.scrollLeft = currentTimeline.scrollLeft - walk;

        // With JQuery it's possible to .each a single object, so no need for special cases.
        updateTimelineBars(currentTimeline);
    }
});

function updateTimelineBars(timelines)
{
    // Need to prevent reflows so we do reading here and writing in the next one, all at once.
    // JQuery's .each is synchronous so we can rely on order.
    var missingId = [];
    var timelineBarIds = [];
    var percentageMargin = [];
    $(timelines).each(function()
    {
        let timelineBarId = $(this).attr("data-timelineBarId");

        if(!timelineBarId)
            missingId.push(true);
        else
            missingId.push(false);
        
        timelineBarIds.push(timelineBarId);
        percentageMargin.push((this.scrollLeft + this.clientWidth / 2) / (this.scrollWidth + this.clientWidth / 2));
    });

    let newSliderBarHTML = $("<div class=\"overview-timeline-slider-bar\"></div>");
    let sliderWidth = $(".overview-timeline-slider").innerWidth()
    $(timelines).each(function(index, value)
    {
        if(missingId[index])
        {
            $(this).attr("data-timelineBarId", timelineId);

            let bar = newSliderBarHTML.appendTo(".overview-timeline-slider");
            bar.attr("data-timelineBarId", timelineId);
            timelineBarIds[index] = timelineId;

            ++timelineId;
        }

        let barElement = $(".overview-timeline-slider-bar[data-timelineBarId=\"" + timelineBarIds[index] + "\"]").first();
        let barWidth = barElement.innerWidth();
        let widthRatio = sliderWidth / barWidth;

        barElement.css({"transform": "translateX(" + (percentageMargin[index] * 100 * widthRatio) + "%)"});
    });
}

$("#main").on("click", ".overview-timeline-right-option", function(e)
{
    if($(this).hasClass("overview-timeline-right-option-remove"))
    {
        let timelineAmount = 0;
        $.each($(".overview-timeline-right-title"), (key, value) =>
        {
            if($(value).text() == $(this).parent().parent().children(".overview-timeline-right-title").text())
                ++timelineAmount;
        });

        $(".overview-timeline-slider-bar[data-timelineBarId=\"" + $(this).parent().parent().parent().attr("data-timelineBarId") + "\"]").remove();
        if (timelineAmount > 1)
            $(this).parent().parent().parent().remove();
        else
            $(this).parent().parent().parent().addClass("disabled");
    }
    else if($(this).hasClass("overview-timeline-right-option-lock"))
    {
        $(this).parent().parent().parent().toggleClass("locked");
    }
});

$("#main").on("click", ".overview-timeline-difficulty", function()
{
    var respectiveTimeline = null;
    $.each($(".overview-timeline-right-title"), (key, value) =>
    {
        if($(value).text() == $(this).text())
        {
            respectiveTimeline = $(value).parent().parent();
            return false;
        }
    });
    
    var html = $(respectiveTimeline)[0].outerHTML;
    var newTimeline = $(html).appendTo($(this).parent().parent().parent().children(".overview-timeline-content"));

    $(newTimeline).removeAttr("data-timelineBarId");
    $(newTimeline).removeClass("disabled");
    if(!$(newTimeline).hasClass("locked"))
        $(newTimeline).addClass("locked");
    
    $(newTimeline).scrollLeft(scrollLeft);
});

function keyHandler(e)
{
    if(e.altKey)
        $(".overview-timeline-content").addClass("altPressed");
    else
        $(".overview-timeline-content").removeClass("altPressed");
}
$(document).keydown(keyHandler);
$(document).keyup(keyHandler);
$(window).focusin(() =>
{
    // Prevents alt-tabbing from causing the app to think alt is held down when window is restored.
    $(".overview-timeline-content").removeClass("altPressed");
});

$("#main").on("click", ".overview-timeline-slider", function(e)
{
    let offsetX = e.pageX - $(this).offset().left;
    let padding = parseFloat($(this).css("padding-left"));

    var percentage = (offsetX - padding) / this.clientWidth;
    if(percentage < 0)
        percentage = 0;
    if(percentage > 1 - padding * 3 / this.clientWidth)
        percentage = 1 - padding * 3 / this.clientWidth;

    let children = $(this).children(".overview-timeline-slider-bar");
    if($(children).length > 0)
    {
        // `.split(/[()]/)[1].split(',')[4]` gets the `translateX` property value.
        var minMargin = parseFloat($(children).first().css("transform").split(/[()]/)[1].split(',')[4]);
        var maxMargin = parseFloat($(children).first().css("transform").split(/[()]/)[1].split(',')[4]);
        var totalMargin = 0;

        $(children).each(function()
        {
            let marginLeft = parseFloat($(this).css("transform").split(/[()]/)[1].split(',')[4]);
            totalMargin += marginLeft;
            if(marginLeft < minMargin)
                minMargin = marginLeft;
            if(marginLeft > maxMargin)
                maxMargin = marginLeft;
        });

        avrMargin = totalMargin / $(children).length;
        marginOffsets = [];
        widths        = [];
        $(children).each(function()
        {
            let marginLeft = parseFloat($(this).css("transform").split(/[()]/)[1].split(',')[4]);
            let width      = parseFloat($(this).css("width"));
            marginOffsets.push(avrMargin - marginLeft);
            widths.push(width);
        });

        scrollWidths = [];
        $(".overview-timeline.locked").each(function(index, value)
        {
            scrollWidths.push(this.scrollWidth);
        });

        // Writing goes here.
        let sliderWidth = children.parent().innerWidth()
        let widthRatio = sliderWidth / widths[0]
        $(children).each(function(index, value)
        {
            $(this).css("transform", "translateX(calc(" + percentage * 100 * widthRatio + "% + " + marginOffsets[index] + "px - " + widths[index] / 2 + "px))");
        });
        $(".overview-timeline.locked").not(".disabled").each(function(index, value)
        {
            this.scrollLeft = scrollWidths[index] * percentage + marginOffsets[index];
        });
    }
});