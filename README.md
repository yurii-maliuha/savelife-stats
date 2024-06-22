
# Save Life Stats

## ToDo list:
 - 1. Data Downloader 
	 - 1. ~~load 10k of data for 01.2023~~ 
	 - 2. load data for Jan 2023
	 - 3. Store worker logs at file (or ES)
	 
 - 2. Data Validator
	 - 1. filter out duplicates
	 - 2. identify possibly missed data by finding the transactions IDs gaps (f.e. ...163690267, 163690269, ... -> 163690268 is missed)
	 - 3. When 1.2 is ready, manually verify the correctness of your calculation by comparing results with SL site
		
- 3. Data Parser
	- 1. [23/3/24]: Right now any english words are defined as fullName (check test TryParseIdentity_ReturnsUnidentified). For each match term make a call to one of dictionary and ignore ones that have definition
		         We can use this endpoint: GET https://www.merriam-webster.com/lapi/v1/mwol-search/autocomplete?r=collegiate-thesaurus&search={word}
	- 2. [23/3/24]: During fullName calculation change string strict comparison to string similarity and allow some % of mistakes (5-7%).
				 Exmaple: transaction 164948299, Зищук Олесандр -> Зищук ОлеКсандр


 - 4. ElasticSearch Indexer
	- 1. ~~add Scaffolfer which creates index~~
	- 2. ~~define mapping for entity~~
	- 3. ~~index first chunk of data~~
	- 4. ~~update ES entity to contains identity and cardholder properties~~
	- 5. update docker compose to preserve index data in volume (if it's possible?)
			- [17/3/24]: Partialy done. The data is stored as internal docker volume. It would be nice to store volume data in git
	- 6. ~~[17/3/24] update Indexed to load data with date > the nearest date among already indexed items~~
	- 7. SaveLife.Stats.Indexer contains both indexing and query processing work. Should we keep this in a single service and implement the pipeline design pattern or move to two separate services so that we can implement
	   simultaneous indexing and query processing using events?

 - 5. Queries
	- 1. ~~identify the number of unique donaters~~
			- ~~create donaters mongodb collection to store agregation~~
	- 2. TransactionsDataAggregator should store the indexDate of the last updated transactions and aggregate just a new records
			- the above should be covered by integration tests
	- 3. Build a new query to calculate the normal distribution by donate amount. The ES histogram aggregations could be used
	- 4. Figure ou the correct way to represent the aggregations results. Should we generate a pdf document or a standalone image per aggregation?

### Priority
- 5.2 fix TransactionsDataAggregator
- 5.3 normal distribution query
- 1.2 load data for Jan 2023 (~40K transactions to download)
- 2.3 manual data verification
- 5.4 UI representation for first two queries

## Scripts for loading names from wikipedia

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
