# Tubes1_pikirnanti
Welcome to team **pikirnanti!** ヾ(•ω•`)o

## Deskripsi Tugas Besar
Tugas Besar 1 Strategi Algoritma bertujuan untuk mengimplementasikan algoritma *greedy* pada *bot* permainan Robocode Tank Royale dengan strategi yang dirancang untuk memperoleh skor setinggi mungkin pada akhir pertempuran. Setiap kelompok membuat empat buah *bot* dalam bahasa C# 🤖

## Penjelasan Singkat Algoritma
Algoritma *greedy* merupakan algoritma yang memecahkan persoalan secara langkah per langkah sedemikian sehingga pada setiap langkah diambil pilihan yang terbaik yang dapat diperoleh pada saat itu tanpa memperhatikan konsekuensi ke depan. </br>
**1. *Bot* Derik** </br>
Derik memiliki strategi utama *locking* pada musuh yang berhasil terdeteksi melalui *radar*. Selama permainan berlangsung, Derik akan bergerak mengitari musuh yang telah di-*lock* dalam pola osilasi yang acak. </br>
**2. *Bot* Botol** </br>
Botol adalah *bot* berbasis algoritma *greedy* dengan strategi yang lebih agresif, yaitu melakukan *locking* dan *ramming* pada musuh yang berhasil terdeteksi melalui *radar*. Setelah target dideteksi, Botol akan secara langsung bergerak mendekat dan menabrak target selagi terus menembakkan peluru. </br>
**3. *Bot* NotThatGuy** </br>
NotThatGuy adalah bot yang menggunakan algoritma greedy dengan strategi utama berupa arah pergerakan yang acak dengan tetap menghindari tembok untuk mencegah *wall damage*. Untuk penembakan, bot NotThatGuy menerapkan penembakan prediktif berdasarkan kecepatan dan arah *bot* musuh. </br>
**4. *Bot* Emo** </br>
Emo adalah *bot* yang berfokus pada menghindari ancaman dengan cara mencari posisi *bot* yang paling sedikit kemungkinan ditembak. Selain itu, *bot* Emo menembak musuh dengan memprediksi kemana *bot* musuh akan bergerak saat peluru tiba. </br>

## Cara Menjalankan Program
Pastikan bahwa kamu memiliki *dependencies* Java terlebih dahulu sebelum menjalankan program! ♪(´▽｀)
1. *Download asset* dari *release* terbaru *repository* berikut: </br>
https://github.com/Ariel-HS/tubes1-if2211-starter-pack/releases/tag/v1.0 </br>
2. *Download file bot* yang ingin dimainkan dari *repository* berikut: </br>
https://github.com/albertchriss/Tubes1_pikirnanti </br>
3. Jalankan *file* .jar aplikasi GUI dengan mengetikkan *command* berikut pada terminal:
```
java -jar robocode-tankroyale-gui-0.30.0.jar
```
4. *Setup* konfigurasi *booter* dengan menekan tombol `Config` lalu `Bot Root Directories`, kemudian masukkan *directory* yang berisi folder-folder *bot* yang ingin dimainkan </br>
![alt text](img/1.png)
![alt text](img/2.png)
5. Jalankan *battle* dengan cara menekan tombol `Battle` lalu `Start Battle`
![alt text](img/3.png)
6. *Boot bot* yang ingin dimainkan, kemudian tekan tombol `Add` untuk menambahkan *bot* ke dalam permainan. *Bot* yang berhasil ditambahkan akan otomatis muncul pada kotak kanan-bawah </br>
![alt text](img/4.png)
7. Untuk memulai permainan, tekan tombol `Start Battle`
![alt text](img/5.png)

## Made by
Albertus Christian Poandy | 13523077 </br>
Grace Evelyn Simon | 13523087 </br>
![alt text](img/6.png)