# GTA5AddOnCarHelper

A tool for installing and managing add-on vehicles into GTA 5

<b><a href="https://www.youtube.com/watch?v=X2rMTfq-VlU" target="_blank">Click Here To Watch The Tutorial</a></b>

<b>Features include:</b>

<b>DLC Extractor</b>
<ul>
<li>Takes a folder containing vehicle downloads in .zip, .rar, or .7zip format, and extracts all of the folders containing the dlc.rpf file into a single folder for easy copy/pasting into the mods/update/x64/dlcpacks folder</li>
<li>Automatically generates DLCList inserts from the extracted folders for easy copy/pasting into the DLCList.xml file</li>
</ul>
<b>Premium Deluxe Auto Manager</b>
<ul>
<li>Allows you to import a list of vehicle.meta files and automatically convert them into .ini files that are compatible with ImNotMentaL's <a href="https://www.gta5-mods.com/scripts/premium-deluxe-motorsports-car-shop" target="_blank">Premium Deluxe Motorsport Car Dealership</a> mod</li>
<li>Once vehicle.meta files are imported, you can edit the Name/Make/Price etc. of the cars to determine how they will appear at the Premium Deluxe Shop in game</li>
<li>Allows bulk editing of cars via different filter options on Make/Class/Price, etc.</li>
<li>Built in Google Search scraper that can automatically pull in vehicle pricing info from the web and automatically assign it to vehicles</li>
<li>The 'Class' value of a vehicle will determine how vehicles are grouped, and will output different .ini files for each class name.  Ex. 'sport.in', 'sedan.ini'</li>
<li>Allows merging and editing of base game .ini files for Premium Deluxe as well</li>
</ul>
<b>Language Generator</b>
<ul>
<li>Takes a folder full of .gxt2 files and attempts to match the hash values to the model and make names from imported vehicle.meta files.  Once mapped, you can edit the in-game display names of vehicle models and makes so that names are no longer missing or incorrect when entering vehicles</li>
<li>Can automatically map missing vehicle make and model names in bulk by parsing data contained within the source download folder</li>
</ul>
<b>Vehicle Meta File Manager</b>
<ul>
<li>Can take a folder full of loose .meta files related to add-on vehicles (vehicles.meta, handling.meta, carcols.meta, etc.) and attempts to filter and organize them into subfolders based on the vehicle model.  This is mainly for future compatibility with mietek_'s <a href="https://www.gta5-mods.com/tools/mmvehiclesmetamerger" target="_blank">mmvehiclesmetamerger</a> mod</li>
</ul>
