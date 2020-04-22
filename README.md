# Smart Previewed Reality


# 概要
ROS-TMSによる，SmartPalの行動計画や，ロボットの動作の物品への影響を物理シミュレーションしたものをARCoreを用いて提示することで，近未来を可視化するAndroidアプリケーション

# 必要な環境
PC1 : Windows10 64bit（アプリケーションビルド用）  
PC2 : Ubuntu 16（Smart Previewed Reality実行用）  
※PC1とPC2は同時に起動する必要なし，デュアルブートでOK

スマートフォン : ARCore対応Android端末（Pixel 4 XL推奨）  
参考：https://developers.google.com/ar/discover/supported-devices?hl=ja

ROS kinetic (Ubuntuにインストールしておく)


# 開発環境
PC : Windows 10 64bit  
* Unity 2018.4.1f1  
* Visual Studio 2017  
* Android Studio 3.5.1  

Android（動作確認済み） : Pixel 3 XL, Pixel 4 XL


# アプリケーションをビルドするためのPCの準備
1. Unityのインストール  
    URL : https://unity3d.com/jp/get-unity/download

1. Visual Studioのインストール  
    ※VS Codeではない  
    ※Unityのインストール中にインストールされるものでOK  
    URL : https://visualstudio.microsoft.com/ja/downloads/

1. Android Studioのインストール  
    ※Android SDKが必要  
    URL : https://developer.android.com/studio


# アプリケーションのインストール方法

## GitHubから丸ごとクローン/ダウンロードする場合（推奨）
1. GitHubから任意の場所にダウンロード

1. Unityでプロジェクトを開く
1. Smart Previewed RealityのSceneを開く
1. File > Build Settingsからビルド環境の設定を開く
1. Androidを選択し，Switch Platformを選択
1. Android端末をPCに接続し，Build & Run


## unitypackageファイルからアプリケーションを作成する場合
1. GitHubから任意の場所にダウンロード

1. Unityで新規プロジェクトを作成
1. Assets > Import Package > Custom Packageを選択し，ダウンロードしたフォルダからSmartPreviewedReality.unitypackageを選択する
1. すべてのAssetsを読み込む
1. Sceneの中にSmart Previewed Realityがあるので，Hierarchyにドラッグアンドドロップ
1. File > Build Settingsからビルド環境の設定を開く
1. Add Open Scenesをクリック
1. Smart Previewed Realityが表示され，チェックが入っていることを確認する  
    * 5を実行しないと表示されない
    * チェックが入ってなければチェックを入れる
    * 他のSceneのチェックは外す
1. Androidを選択し，Switch Platformを選択
1. Player Settingsを開く
    * Resolution and Presentation
        * Allowed Orientations for Auto Rotation
            * Portrait : **false**
            * Portrait Upside Down : **false**
            * Landscape Right : **true**
            * Landscape Left : **true**
    * Other Settings
        * Rendering
            * Color Space : **Gamma**
            * Auto Graphics API : **true**
            * Multithreaded Rendering : **false**
            * Static Baching : **true**
            * Dynamic Batching : **false**
            * GPU Skinning : **true**
            * Graphics Jobs : **false**
            * Lightmap Streaming Enable : **true**
            * Streaming Priority : **0**
            * Protect Graphics Memory : **false**
            * Enable Frame Timing Stats : **false**
        * Identification
            * Package Name : (他と被らないように決める)
            * Minimum API Level : **Android 7.0 'Nougat'**
            * Target API Level : **Automatic (highest installed)**
    * XR Settings
        * Virtual Reality Supported : **false**
        * ARCore Supported : **true**
1. Android端末をPCに接続し，Build & Run

# 使い方

## ROS-TMS for Smart Previewed Realityの実行

実行前に，ROSをインストールしたUbuntuでROS-TMS for Smart Previewed Realityをcatkin_makeしておく必要がある．

ROS-TMS for Smart Previewed Reality : https://github.com/SigmaHayashi/ros_tms  

このアプリケーションをフルに利用するためには，B-sen，SmartPal V，Viconが必要である．
また，データベースを利用するため，mongodbをインストールする必要がある．その他依存関係はROS-TMSのWikiを参照．
Wiki : https://github.com/irvs/ros_tms/wiki

Smart Previewed RealityにはAndroid端末へのプッシュ通知機能があるが，ここでは通知機能を用いない実行方法を説明する．プッシュ通知を利用して使用する場合は以下を参照．  
tms_ur_notification : [数日中にアップします，少々お待ちください．]()

### 実行手順
```
$ roscore
$ roslaunch rosbridge_server rosbridge_websocket.launch
$ rosrun tms_ss_vicon vicon_stream
$ roslaunch tms_db_manager tms_db_manager.launch
$ roslaunch tms_ts_ts task_scheduler_for_preview.launch
$ roslaunch tms_rp_voronoi_map voronoi.launch

// 以下は移動のみの実行なら不要な操作
// 物品取り寄せタスクを実行する際には必要
$ roslaunch rostms_bringup smartpal_moveit.launch
$ roslaunch smartpal5_arm_navigation subtask.launch
$ rosrun tms_rc_smartpal_control_unity smartpal_control_unity.py
```

以下の1，2のどちらかを実行する
1. SmartPal Vを実世界で動作させる場合  
以下のコンソールの数だけSSHでSmartPal搭載NUCにアクセスする．  
※SmartPal搭載NUCにもROS-TMSをインストールする必要あり
```
$ ./start_omniNames

// ここでSmartPalのスイッチパネルのVehicle/Armsスイッチを入れる
// 15秒待ってから

$ rosrun tms_rc_smartpal_control smartpal5_control
```

1. SmartPal Vを動かさずにアプリの動作を確認する場合  
※仮想的にSmartPalの応答が発行される
```
$ rosrun tms_rc_smartpal_virtual_control smartpal5_virtual_control
```

## アプリの操作

1. Smart Previewed Realityアプリケーションを起動

    ※初回起動時は，Settingsボタンを押してROS-TMSを実行しているUbuntu PCのIPアドレスを指定する必要あり（うまく起動しない場合はWi-Fiを一度オフにしてからアプリを起動するとスムーズに起動するかも）

1. 以下のどちらかの方法で位置合わせを行う
    1. 起動直後の画面のまま画像マーカを認識させる

    1. Self Localizationボタンを押して手動で位置合わせを行う
        1. 自分がいる位置をUIで選択し，OK
        1. 自分が向いている方向をUIで選択し，OK
        1. 自分が端末を持っている高さにスライドバーを合わせて，OK

1. 自己位置の微調整を行う
    1. Calibrationボタンを押す
    1. XYZ方向の移動，回転を調整する
    1. Back to Mainボタンを押す

## サービスの実行

以下のコマンドを直接入力するか，Previewed Reality Service Callerアプリを使うことで実行できる．

Previewed Reality Service Callerアプリを使うためには別のAndroid端末が必要（ARCore対応端末でなくてOK）

Previewed Reality Service Caller : [数日中にアップします，少々お待ちください．]()

* 移動サービス  
    ※SmartPalがキッチンに向かう
```
$ rosservice call /tms_ts_master "{rostime: 0, task_id: 8007, robot_id: 2003, object_id: 0, user_id: 0, place_id: 6017, priority: 0}"
```

* 物品取り寄せサービス  
    ※SmartPalがキッチンにあるチップスターを取りに行き，テーブル付近に持っていく  
    ※データベースに格納されている情報次第で動作は変わる
```
$ rosservice call /tms_ts_master "{rostime: 0, task_id: 8001, robot_id: 2003, object_id: 7001, user_id: 1001, place_id: 0, priority: 0}"
```
