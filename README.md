Popis projektu
Tento projekt je jednoduchá ASP.NET Core aplikace, která umožňuje detekovat změny ve zvoleném adresáři. Při prvním spuštění aplikace analyzuje obsah zvoleného adresáře a ukládá jeho stav. Při každém dalším spuštění dokáže zjistit:

Nové soubory a adresáře
Změněné soubory a adresare
Odstraněné soubory a adresáře
Každý soubor a adresar je sledován podle svého obsahu prostřednictvím SHA256 hashe. Verze se zvýší pokaždé, když se změní obsah souboru nebo date modified pro adresar

Sledování souborů a adresářů – Detekce nových, změněných nebo smazaných souborů.
Podpora více adresářů – Informace o všech analyzovaných adresářích jsou ukládány a lze je sledovat samostatně.
Omezení velikosti souboru a počtu souborů – Soubor větší než 50 MB je přeskočen a maximálně je analyzováno 100 souborů v jednom adresáři.

Pro zlepseni: 
  - pokud ukládáme všechny adresáře do jednoho JSON souboru, může se při velkém počtu adresářů zpomalit čtení a zápis - pro kazdou cesty - novy json
  - krome logovani prohazet chyby nahoru do front endu
  - logovani a zjisteni dalsich exceptionu
