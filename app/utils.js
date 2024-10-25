// Contains general utility functions

window.$ = window.jQuery = require('jquery');

const fs = require('fs');
const path = require('path');
const util = require('util');

const readdirAsync = util.promisify(fs.readdir);
const statAsync = util.promisify(fs.stat);

module.exports =
{
	/// Decodes special HTML characters such as &amp; into &.
	decodeHtml: function(specialchars)
	{
		var textArea = document.createElement('textarea');
		textArea.innerHTML = specialchars;
		return textArea.value;
	},

	/// Returns a list of folder and file paths.
	getDirectory: async function(folderPath, foldersOnly = false)
	{
		const basepath = path.join(folderPath);
		const files = await readdirAsync(basepath);
		const stats = await Promise.all(
				files.map((filename) =>
					statAsync(path.join(basepath, filename))
						.then((stat) => ({ filename, stat }))
				)
			);
		const sortedFiles = stats.sort((b, a) =>
				a.stat.mtime.getTime() - b.stat.mtime.getTime()
			).filter((stat) =>
				stat.stat.isDirectory() || !foldersOnly
			).map((stat) => stat.filename);

		return sortedFiles;
	},

	/// Saves a value to a key in a file, creates one if it doesn't exist.
	saveFile: function(filePath, key, value)
	{
		var read = "";
		if(fs.existsSync(filePath))
			read = fs.readFileSync(filePath);
		
		var content = "";
		var success = false;
		
		if(read != "")
		{
			var splits 	= (read + "").split("\r\n");
			for(var i = 0; i < splits.length; ++i)
			{
				if(splits[i].startsWith(key + ": "))
				{
					content += key + ": " + value + "\r\n";
					success = true;
				}
				else if(splits[i].length > 0)
					content += splits[i] + "\r\n";
			}
		}
		
		if(!success)
			content += key + ": " + value + "\r\n";
		
		try
		{
			fs.writeFileSync(filePath, content, 'utf-8');
		}
		catch (exception)
		{
			console.log(exception.message);
		}
	},

	/// Reads a value from a key in a file, returns an empty string if none exists.
	readFile: function(filePath, key)
	{
		if(fs.existsSync(filePath))
		{
			var read = fs.readFileSync(filePath);
			
			var splits 	= (read + "").split("\r\n");
			for(var i = 0; i < splits.length; ++i)
				if(splits[i].startsWith(key + ": "))
					return splits[i].substr(key.length + ": ".length);
		}
		return "";
	},
	
	/// Returns the substring between two substrings in a string.
	scrape: function(data, start, end)
	{
		return data.substring(data.indexOf(start) + start.length).substring(0, data.substring(data.indexOf(start) + start.length).indexOf(end));
	},

	/// Escapes the data for use in data tags, adding backslashes in front of quotes, etc.
	escape: function(data)
	{
		return (data + '').replace(/[\\"']/g, '\\$&').replace(/\u0000/g, '\\0');
	}
};

