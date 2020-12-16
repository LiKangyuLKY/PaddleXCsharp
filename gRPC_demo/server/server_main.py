import sys,os
# sys.path.append('../Packages')
# sys.path.append(os.path.join(os.getcwd(),'..'))
from concurrent import futures
import time
import grpc
from example import PaddleXserver_pb2_grpc
from server.Server_core import PaddleXserver

def grpc_server(channel):
    #启动 rpc 服务
    server = grpc.server(futures.ThreadPoolExecutor(max_workers=10))  #创建服务池（python线程池）
    PaddleXserver_pb2_grpc.add_PaddleXserverServicer_to_server(PaddleXserver(),server)
    server.add_insecure_port(channel)
    return server
        
if __name__ == '__main__':
    server = grpc_server("0.0.0.0:54888")
    server.start()
    print('server start')
    try:
        while True:
            time.sleep(60*60*24) #一天，按秒算
    except KeyboardInterrupt:
        server.stop(0)