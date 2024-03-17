
# Save Life Stats

## ToDo list:

 - Data Downloader 
	 - ~~load 10k of data for 01.2023~~ 
	 - load data for 01-03 2023
	 - Store worker logs at file (or ES)
	 
 - Data Validator
	 - filter out duplicates
	 - identify possibly missed data by finding the transactions IDs gaps (f.e. ...163690267, 163690269, ... -> 163690268 is missed)


 - ElasticSearch Indexer
	- ~~add Scaffolfer which creates index~~
	- ~~define mapping for entity~~
	- ~~index first chunk of data~~
	- update ES entity to contains identity and cardholder properties
	- update docker compose to preserve index data in volume (if it's possible?)
		- [17/3/24]: Partialy done. The data is stored as internal docker volume. It would be nice to store volume data in git
	- [17/3/24] update Indexed to load data with date > the nearest date among already indexed items

 - ES Queries
	- identify the number of unuque donaters


Scripts for loading names from wikipedia

https://uk.wikipedia.org/wiki/%D0%A1%D0%BF%D0%B8%D1%81%D0%BE%D0%BA_%D1%83%D0%BA%D1%80%D0%B0%D1%97%D0%BD%D1%81%D1%8C%D0%BA%D0%B8%D1%85_%D1%87%D0%BE%D0%BB%D0%BE%D0%B2%D1%96%D1%87%D0%B8%D1%85_%D1%96%D0%BC%D0%B5%D0%BD
```js
allNames = [];
var stop = false;
$('.mw-content-ltr li:not([class^="toclevel-1"])').each(function(){
  var nameStr = $(this).text();
  if(nameStr.length > 15)
  {
	  return;
  }
  if(!stop) {
	  var names = nameStr.split(' ').map(name => name.replaceAll('(', '').replaceAll(')', '').replaceAll(',',''));
		allNames.push(...names);  
  }
});
console.log(JSON.stringify(allNames))
```

https://uk.wikipedia.org/wiki/%D0%A1%D0%BF%D0%B8%D1%81%D0%BE%D0%BA_%D1%83%D0%BA%D1%80%D0%B0%D1%97%D0%BD%D1%81%D1%8C%D0%BA%D0%B8%D1%85_%D0%B6%D1%96%D0%BD%D0%BE%D1%87%D0%B8%D1%85_%D1%96%D0%BC%D0%B5%D0%BD
```js
allNames = [];
var stop = false;
$('.wikitable tr td:first-child, .wikitable tr td:nth-child(3)').each(function(){
  var nameStr = $(this).text();
  if(nameStr.startsWith("Ім'я") || nameStr.startsWith("Варіанти"))
  {
	  return;
  }
  var names = nameStr
	.replaceAll(new RegExp(/(?=['iіжєїa-я]+)[IІЖЄЇА-Я]{1}/gm),(a, b) => ' ' + a)
	.replaceAll(',','')
	.trim().split(' ')
	.map(name => name.replaceAll('\n', ''))
	.filter(x => x.length > 0);
	
  allNames.push(...names);
});
console.log(JSON.stringify(allNames))
```
