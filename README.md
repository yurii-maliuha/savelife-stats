
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

 - ES Queries
	- identify the number of unuque donaters


