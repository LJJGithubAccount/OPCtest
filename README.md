# Opc Demo说明文档
本demo仅作通信以及数据类型验证
## 项目简介

- /lib 包含三个必要的库文件
- /demo opc demo项目  

软件打包列表  

- Interop.NetFwTypeLib.dll
- Opc.Ua.Configuration.dll
- Opc.Ua.Core.dll
- Opc.Ua.Server.dll
- ServerCmd.exe 
- ServerCmd.Config.xml  

### 测试
使用opc基金会提供的测试工具对本demo进行测试，测试结果保存在**test.txt**和**test1.txt**，作为自定义的节点和读写均通过了测试。测试结果有四项未通过测试，未通过的测试的内容主要是自带的服务器信息节点。对于本项目来说，这些不影响本服务器的使用和性能。  
## 示例节点
在**TEST**节点下，有一个类型测试节点**TypeTest**，包含所有本服务的测试节点，节点类型分别为  

- Int32
- Float
- Double
- DateTime
- Boolean
- Guid
- String
- NodeId
- StringArray
- IntNoWrite  

前八个包含了C#常见的数据类型,第九个是使用字符串类型来显示数组类型，最后一个是只读类型，不允许写入数据，前九个数值都没有变化，最后一个每秒重置一个随机数，可以进行订阅测试。  
## 配置文件
配置文件名为**ServerCmd.Config.xml**，在配置文件中**ServerConfiguration**节点下的**BaseAddresses**节点中可以更改IP地址和端口号等。
## 用户名及密码
用户名: user
密码: password