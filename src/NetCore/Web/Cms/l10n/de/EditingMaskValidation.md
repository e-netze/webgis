#field_name: Field Name

Feld, das vor dem Speichern geprüft werden sollte

#operator: Operator

Methode, mit der der Feld Wert zum Validator geprüft wird: zB: Ident (=genau gleich), 
Equals (=gleich aber case-insensitiv), in (Feld Wert muss in der Liste der Validator Werte vorkommen), 
inside (einer der Feld Werte muss in einem der Validator Werte vorkommen), IN/INSIDE (wie in/inside allerderins case-sensitiv). 
Kommen im Feldwert oder im Validator Werte ',' oder ';' vor, werden diese als Trennzeichen verwendet und der Wert als Liste interpretiert.

#validator: Validator

Wert auf den geprüft wird. Hier kann auch auf eine Liste mit Trennzeichen (, oder ;) angegeben werden. 
Außerdem sind Platzhalter für User-Rollen möglich: role-parameter:GEMNR

#message: Message

Nachricht, die ausgeben wird, wenn Validation fehl schlägt

