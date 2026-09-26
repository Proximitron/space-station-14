# Copilot Instructions

## Projektrichtlinien
- Für den Remote-Borg müssen zwei Ebenen getrennt behandelt werden: Erst werden Borg-Modul-/Set-Auswahlknöpfe als Aktionen auf dem Chassis angezeigt; nach Auswahl stellt das Set mehrere Werkzeug-Gegenstände als Hände bereit. Der Remote-Pfad muss daher zuerst die Set-Aktionen übertragen und danach die normalen Hand-/Item-Interaktionen des ausgewählten Sets nutzen.
- Änderungen sollten zunächst gezielt geprüft werden, um lange Build-Dauern zu vermeiden. Führen Sie vollständige Projekt-Builds gesammelt erst am Ende aus.
- Der Nutzer bevorzugt, Bedingungen und bestehende Prüfpfade direkt zu erweitern (z.B. Remote- oder Mind-Bedingung zu akzeptieren), statt zusätzliche äußere Workarounds oder nachgelagerte Synchronisationslogik zu bauen.
- Bei RemoteControl soll das normale Borg-Verhalten exakt erhalten bleiben: X wechselt das aktive Gerät/Modul; ein Weltklick wechselt niemals das Gerät, sondern verwendet das aktuell aktive Tool für Menüöffnung und Item-auf-Ziel-Interaktionen. Remote-spezifische Workarounds oder Klick-Auswahl von Geräten sind unerwünscht.

## Fehleranalyse
- Bei der Analyse von Prediction-Bugs berücksichtigen Sie, dass wiederholte Client-Open/Dispose-Logs durch Prediction-Replay und Rollback entstehen können; eine zunehmende Loganzahl beweist keine akkumulierten Handler. Beobachtungen im Spiel sind gegenüber einer statischen Annahme über den Input-Pfad maßgeblich.
