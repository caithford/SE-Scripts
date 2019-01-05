/*
 *   R e a d m e
 *   -----------
 * 
 *   In this file you can include any instructions or other comments you want to have injected onto the 
 *   top of your final script. You can safely delete this file if you do not want any such comments.
 * 
 * Concepts:
 * place the word "Airlock" followed by an alphanumeric identifier in the customdata field of each block the airlock is to utilize
 * 
 * Pick a "master" air vent for the main configuration of each airlock.
 * 
 * O2 Bottles - designate bottles to be controlled by the script
 * O2 Generators - designate generators to be controlled by the script
 * LCD Panels - assign LCD's to airlocks (and optionally, to doors)
 * Air Vents - 
 * Doors - 
 * Lights - 
 * Sound Blocks - 
 * Sensor Blocks
 * 
 * 
 * Air Vent
 * Airlock:<id>,master|slave,name;
 * 
 * Door
 * Airlock:<id>,<doorid>,name;
 * 
 * Other
 * Airlock:<id>,<doorid>; (assigned to door, outside)
 * Airlock:<id>; (assigned to airlock, inside)
 */