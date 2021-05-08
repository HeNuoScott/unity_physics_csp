# unity_physics_csp

Use Unity 2019.4.12f1 or up

演示项目显示了在Unity中客户端预测的基本实现，所以真正的网络发生，所有网络流量在一个单一的Unity实例中模拟。  

此项目 重在演示客户端 如何做预测本人暂时未整理出 多人预测逻辑

项目存在弊端：
由于客户端做预测执行输入时服务器未进行输入，当服务器输入时需要将客户端的输入 执行一遍，这样就造成了客户端的输入驱动服务器运行，多个客户端模拟运行失败！！

**参考**:
* [NetCode-FPS](https://github.com/Yinmany/NetCode-FPS)
* [Client-Side Prediction With Physics in Unity](http://www.codersblock.org/blog/client-side-prediction-in-unity-2018)

## 运行Demo

Build Setting 添加
LocalEditorSceneTest
server_physics_scene
client_physics_scene

运行 LocalEditorSceneTest 场景