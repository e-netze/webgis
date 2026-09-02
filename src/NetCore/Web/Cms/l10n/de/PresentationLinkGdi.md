#visible: Sichtbar

Darstellungsvariante ist für den Anwender sichtbar/schaltbar

#category_visible: Allgemein

#client_visibility: Sichtbar, falls Client

Hier kann eingeschränkt werden, ob eine Darstellungsvariante nur auf einem bestimmten Endgerät
angezeigt wird.

#category_client_visibility: Sichtbarkeit

#is_container_default: Default für Container

#category_is_container_default: Container

#visible_with_service: Sichtbar, wenn dieser Dienst in Karte

Die Anzeige einer Darstellungsvariante machte nicht immer Sinn. Möchte man zB beim Einschalten
einer Darstellungsvariante (zB Naturbestand) Themen aus einem anderen Dienst (zB Kataster) ausschalten,
hat es keinen Sinn, wenn die Container angezeigt wird, wenn nur der Kataster Dienste in einer Karte vorkommt.
Für diesen Fall kann man hier diese Option ausschalten. Die Eigentliche Gruppe wird dann nur angezeigt,
wenn sich auch der Dienst (zB Naturbestand) in der Karte eingebunden ist.

#category_visible_with_service: Gruppe

#visible_with_one_of_services: Sichtbar, wenn einer dieser Dienste in der Karte vorkommt

Liste der Service-Urls mit Beistrich getrennt

#category_visible_with_one_of_services: Gruppe

#metadata_link: Metadaten Link

Wird im Viewer als [i] Button dargestellt und verweißt auf angefühten Link. Im Link können die Platzhalter
für die Karte, wie bei benutzerdefnierten Werkzeugen verwendet weden: {map.bbox}, {map.centerx}, {map.centery}, {map.scale}

#category_metadata_link: Metadaten

#metadata_target: Metadaten Target

Gibt an, wie der Link geöffnet wird (tab => neuer Tab, dialog => in Dialogfenster im Viewer).

#category_metadata_target: Metadaten

#metadata_dialog_width: Metadaten Dialog Breite (Width)

Breite des Dialogs (Pixel), wenn type 'dialog'. 0 => default width

#category_metadata_dialog_width: Metadaten

#metadata_dialog_height: Metadaten Dialog Höhe (Height)

Höhe des Dialogs (Pixel), wenn type 'dialog'. 0 => default height

#category_metadata_dialog_height: Metadaten

#metadata_title: Metadaten Titel

Hier kann ein Titel für den Metadaten Button angeben werden.

#category_metadata_title: Metadaten

#metadata_link_button_style: Metadaten Button Style

Gibt an, wie der Button dargestellt wird: [i] Button oder auffälliger Link Button mit Titel.

#category_metadata_link_button_style: Metadaten

#u_i_group_name: Gruppierung

Der Darstellungsvarianten Baum besteht aus Container (übergeordnetes Element) und den eigentlichen
Darstellungsvarianten, die sich wiederum in einer (aufklappbaren) Gruppe befinden können.
Mehre Ebenen werden standardmäßig nicht angeboten, damit der Anwender nicht zu viele Ebenen klicken muss.
Eine weiter Ebene wird darum hier in der Oberfläche nicht angeboten. Allerdings gibt es immer wieder
Ausnahmen, bei der eine weitere Ebene die Benutzerelemente im Viewer schlanker und einfache machen kann.
Für diese Ausnahmen ist es möchglich, hier noch eine weiter Gruppierung anzugeben. Der hier
angegebene Name entspricht dem Namen einer weiteren aufklappbaren Gruppe, die im Darstellungsvarianten
Baum dargestellt wird. Mehre Darstellungsvarianten in der aktuellen Ebnen können hier den selben Gruppennamen
aufweisen und werden unter dieser Gruppe angezeigt. Achtung: der hier angeführte Wert sollte in der Regel leer sein,
außer eine weiter Gruppierung bringt für die Bedienung Vorteile. Der hier eingetrage Wert wird später nur für
Darstellungsvarianten berücksichtigit, die sich bereits in der aufklappbaren Gruppe befinden. Befindet sich die
Darstellungsvariante in der obersten Ebene des Containers, bleibit dieser Wert unberücksichtigt.
Die Weg ist hier eine Gruppe zu erstellen und die Darstellungsvariante dort abzulegen!
Es können mehrere Ebenen angegeben werden. Das Trennzeichen ist ein Schrägstrich (/). Solle ein '/' als Text
vorkommen ist dieser mittels '\\/ zu kodieren.'

#category_u_i_group_name: User Interface
