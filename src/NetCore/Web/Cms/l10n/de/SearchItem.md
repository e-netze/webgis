#method: Abfrage Methode

#category_method: Allgemein

#visible: Sichtbar

Gibt an, ob das Feld in der Suchmaske angezeigt wird. Nicht sichbare Felder sind beispielsweise sinnvoll, 
wenn diese nur für parametrierte Aufrufe verwendet werden sollten, für den Anwender aber nicht sichtbar 
sein sollten (z.B. Adresscode, ObjectID, ...).

#category_visible: Allgemein

#required: Eingabe erforderlich

Gibt an, ob die Eingabe dieses Feldes erforderlich ist, um die Abfrage auszuführen.

#category_required: Allgemein

#examples: Beispiele für die Eingabe

Dieser Text wird unter dem Eingabefeld angegzeigt und soll dem Anwender Beispielwerte für die 
Eingabe vermitteln.

#category_examples: Eingabe

#regular_expression: Regulärer Ausdruck

Dieser Ausdruck wird für die Validierung der Eingabe verwendet.

#category_regular_expression: Eingabe

#format_expression: Format Expression

Mit dieser Expression wird die Eingabe formatiert. {0} ist der Platzhalter für die Usereingabe, 
zB DATE '{0}' => wird zu DATE '2015-5-3'. Achtung: Wenn eine Expression angeben wird, muss der 
komplette Ausdruck angegeben inklusive (einfachen) Hochkomma am Anfang oder Ende angeben 
werden! zB 'fix_prefix_{0}_fix_postfix'.

#category_format_expression: Eingabe

#look_up: Auswahlliste

Auswahlliste für dieses Suchfeld.

#category_look_up: Auswahlliste

#use_look_up: Auswahlliste verwenden

Auswahlliste für dieses Suchfeld anwenden.

#category_use_look_up: Auswahlliste

#min_input_length: Minimale Zeicheneingabe

Ab der Eingabe von 'x' Zeichen wird die die Auswahlliste erstellt.

#category_min_input_length: Auswahlliste

#sql_injection_white_list: Sql Injektion Whitelist

Hier kann ein String mit Zeichen angegeben werden, die von der SQL-Injektion überprüfung 
ignoriert werden. zB: ><&'\"

#category_sql_injection_white_list: Sicherheit

#ignore_in_preview_text: In Ergebnisvorschau ignorieren

Werden mehrere Objekte bei einer Abfrage gefunden, wird zuerst eine verfachte Liste der 
Objekte angezeigt. Dazu wird für jedes Objekte ein kurzer Vorschau-Text erstellt. Dieser 
Text setzt sich in der Regel aus den Attributwerten der möglichen Suchbegriffe zusammen. 
Wenn eine Suchbegriff nicht für den Vorschau-Text verwendet werden sollte, kann er hier weggeschalten werden.

#category_ignore_in_preview_text: Ergebnisvorschau

#use_upper: SQL-Upper verwenden (Oracle)

Um bei Oracle Datenbanken nicht Case-Sensitiv zu suchen, kann für String Felder SQL-Upper 
auf 'true' gesetzt werden. Liegt eine SQL Server Datenbank zugrunde, ist der Wert immer auf 'false' zu setzen.

#category_use_upper: SQL

