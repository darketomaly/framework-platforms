# framework.platforms

# How to install

Install via git url:
https://github.com/darketomaly/framework-platforms.git?path=/Assets/com.darketomaly.framework.platforms

# Dependencies
Note that these need to be installed before installing the platform package.

1. Playfab
2. ParrelSync
3. Facepunch steamworks (if developing on Steam). Use #STEAMWORKS define symbol

# How to use

After installing, inject the platform config module into the config scriptable via Tools/Framework/Inject platform project config:

<img width="581" height="439" alt="image" src="https://github.com/user-attachments/assets/1fdc3e11-1feb-4e6f-bb30-deca90b1498b" />

Add backend component and platform manager into a gameobject that never dies. As a child, add any desired platform component. These contain a scriptable with some platform data:

<img width="437" height="297" alt="image" src="https://github.com/user-attachments/assets/2b754671-2e4a-4455-9363-a348397c530f" />
