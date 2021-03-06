/* ESP32 implementation of NetworkInterfaceAPI
 * Copyright (c) 2015 ARM Limited
 * Copyright (c) 2017 Renesas Electronics Corporation
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#include "mbed.h"
#include "ESP32Stack.h"

// ESP32Stack implementation
ESP32Stack::ESP32Stack(PinName en, PinName io0, PinName tx, PinName rx, bool debug,
    PinName rts, PinName cts, int baudrate)
{
    _esp = TestBench->esp32_init(en, io0, tx, rx, debug, rts, cts, baudrate);
    memset(_local_ports, 0, sizeof(_local_ports));
}

struct esp32_socket {
    int id;
    nsapi_protocol_t proto;
    bool connected;
    SocketAddress addr;
    int keepalive; // TCP
    bool accept_id;
    bool tcp_server;
};

int ESP32Stack::socket_open(void **handle, nsapi_protocol_t proto)
{
    // Look for an unused socket
    int id = _esp->get_free_id();

    if (id == -1) {
        return NSAPI_ERROR_NO_SOCKET;
    }

    struct esp32_socket *socket = new struct esp32_socket;
    if (!socket) {
        return NSAPI_ERROR_NO_SOCKET;
    }

    socket->id = id;
    socket->proto = proto;
    socket->connected = false;
    socket->keepalive = 0;
    socket->accept_id = false;
    socket->tcp_server = false;
    *handle = socket;
    return 0;
}

int ESP32Stack::socket_close(void *handle)
{
    struct esp32_socket *socket = (struct esp32_socket *)handle;
    int err = 0;

    if (!socket) {
        return NSAPI_ERROR_NO_SOCKET;
    }

    if (!_esp->close(socket->id, socket->accept_id)) {
        err = NSAPI_ERROR_DEVICE_ERROR;
    }

    if (socket->tcp_server) {
        _esp->del_server();
    }
    _local_ports[socket->id] = 0;

    delete socket;
    return err;
}

int ESP32Stack::socket_bind(void *handle, const SocketAddress &address)
{
    struct esp32_socket *socket = (struct esp32_socket *)handle;

    if (!socket) {
        return NSAPI_ERROR_NO_SOCKET;
    }

    if (socket->proto == NSAPI_UDP) {
        if (address.get_addr().version != NSAPI_UNSPEC) {
            return NSAPI_ERROR_UNSUPPORTED;
        }

        for (int id = 0; id < ESP32_SOCKET_COUNT; id++) {
            if (_local_ports[id] == address.get_port() && id != socket->id) { // Port already reserved by another socket
                return NSAPI_ERROR_PARAMETER;
            } else if (id == socket->id && socket->connected) {
                return NSAPI_ERROR_PARAMETER;
            }
        }
        _local_ports[socket->id] = address.get_port();
        return 0;
    }

    socket->addr = address;
    return 0;
}

int ESP32Stack::socket_listen(void *handle, int backlog)
{
    struct esp32_socket *socket = (struct esp32_socket *)handle;

    (void)backlog;

    if (!socket) {
        return NSAPI_ERROR_NO_SOCKET;
    }

    if (socket->proto != NSAPI_TCP) {
        return NSAPI_ERROR_UNSUPPORTED;
    }

    if (!_esp->cre_server(socket->addr.get_port())) {
        return NSAPI_ERROR_DEVICE_ERROR;
    }

    socket->tcp_server = true;
    return 0;
}

int ESP32Stack::socket_connect(void *handle, const SocketAddress &addr)
{
    struct esp32_socket *socket = (struct esp32_socket *)handle;

    if (!socket) {
        return NSAPI_ERROR_NO_SOCKET;
    }

    if (socket->proto == NSAPI_UDP) {
        if (!_esp->open((LPSTR)"UDP", socket->id, (LPSTR)addr.get_ip_address(), addr.get_port(), _local_ports[socket->id])) {
            return NSAPI_ERROR_DEVICE_ERROR;
        }
    } else {
        if (!_esp->open((LPSTR)"TCP", socket->id, (LPSTR)addr.get_ip_address(), addr.get_port(), socket->keepalive)) {
            return NSAPI_ERROR_DEVICE_ERROR;
        }
    }

    socket->connected = true;
    return 0;
}

int ESP32Stack::socket_accept(void *server, void **socket, SocketAddress *addr)
{
    struct esp32_socket *socket_new = new struct esp32_socket;
    long id;

    if (!socket_new) {
        return NSAPI_ERROR_NO_SOCKET;
    }

    if (!_esp->accept(&id)) {
        delete socket_new;
        return NSAPI_ERROR_NO_SOCKET;
    }

    socket_new->id = id;
    socket_new->proto = NSAPI_TCP;
    socket_new->connected = true;
    socket_new->accept_id = true;
    socket_new->tcp_server = false;
    *socket = socket_new;

    return 0;
}

int ESP32Stack::socket_send(void *handle, const void *data, unsigned size)
{
    struct esp32_socket *socket = (struct esp32_socket *)handle;

    if (!socket) {
        return NSAPI_ERROR_NO_SOCKET;
    }

    if (!_esp->send(socket->id, (uint8_t *)data, size)) {
        return NSAPI_ERROR_DEVICE_ERROR;
    }

    return size;
}

int ESP32Stack::socket_recv(void *handle, void *data, unsigned size)
{
    struct esp32_socket *socket = (struct esp32_socket *)handle;

    if (!socket) {
        return NSAPI_ERROR_NO_SOCKET;
    }

    int32_t recv = _esp->recv(socket->id, (uint8_t *)data, size, -1);
    if (recv == -1) {
        return NSAPI_ERROR_WOULD_BLOCK;
    } else if (recv < 0) {
        return NSAPI_ERROR_NO_SOCKET;
    } else {
        // do nothing
    }

    return recv;
}

int ESP32Stack::socket_sendto(void *handle, const SocketAddress &addr, const void *data, unsigned size)
{
    struct esp32_socket *socket = (struct esp32_socket *)handle;

    if (!socket) {
        return NSAPI_ERROR_NO_SOCKET;
    }

    if (socket->connected && socket->addr != addr) {
        if (!_esp->close(socket->id, socket->accept_id)) {
            return NSAPI_ERROR_DEVICE_ERROR;
        }
        socket->connected = false;
    }

    if (!socket->connected) {
        int err = socket_connect(socket, addr);
        if (err < 0) {
            return err;
        }
        socket->addr = addr;
    }

    return socket_send(socket, data, size);
}

int ESP32Stack::socket_recvfrom(void *handle, SocketAddress *addr, void *data, unsigned size)
{
    struct esp32_socket *socket = (struct esp32_socket *)handle;

    if (!socket) {
        return NSAPI_ERROR_NO_SOCKET;
    }

    int ret = socket_recv(socket, data, size);
    if (ret >= 0 && addr) {
        *addr = socket->addr;
    }

    return ret;
}

void ESP32Stack::socket_attach(void *handle, void(*callback)(void *), void *data)
{
    struct esp32_socket *socket = (struct esp32_socket *)handle;

    if (!socket) {
        return;
    }

    _esp->socket_attach(socket->id, (intptr_t)callback, (intptr_t)data);
}

nsapi_error_t ESP32Stack::setsockopt(nsapi_socket_t handle, int level,
    int optname, const void *optval, unsigned optlen)
{
    struct esp32_socket *socket = (struct esp32_socket *)handle;

    if (!optlen) {
        return NSAPI_ERROR_PARAMETER;
    } else if (!socket) {
        return NSAPI_ERROR_NO_SOCKET;
    }

    switch (optname) {
        case NSAPI_KEEPALIVE:
            if (level == NSAPI_SOCKET && socket->proto == NSAPI_TCP) {
                if (socket->connected) {// ESP32 limitation, keepalive needs to be given before connecting
                    return NSAPI_ERROR_UNSUPPORTED;
                }

                if (optlen == sizeof(int)) {
                    int secs = *(int *)optval;
                    if (secs >= 0 && secs <= 7200) {
                        socket->keepalive = secs;
                        _esp->socket_setopt_i(socket->id, (LPSTR)"TCP_KEEPALIVE", secs);
                        return NSAPI_ERROR_OK;
                    }
                }
                return NSAPI_ERROR_PARAMETER;
            }
            break;
        case NSAPI_REUSEADDR:
            if (level == NSAPI_SOCKET && socket->proto == NSAPI_TCP) {
                long val = *(long *)optval;
                if (_esp->socket_setopt_i(socket->id, (LPSTR)"SO_REUSEADDR", val)) {
                    return NSAPI_ERROR_OK;
                }
            }
            break;
        case NSAPI_KEEPIDLE:
            if (level == NSAPI_SOCKET && socket->proto == NSAPI_TCP) {
                long val = *(long *)optval;
                if (_esp->socket_setopt_i(socket->id, (LPSTR)"TCP_KEEPIDLE", val)) {
                    return NSAPI_ERROR_OK;
                }
            }
            break;
        case NSAPI_KEEPINTVL:
            if (level == NSAPI_SOCKET && socket->proto == NSAPI_TCP) {
                long val = *(long *)optval;
                if (_esp->socket_setopt_i(socket->id, (LPSTR)"TCP_KEEPINTVL", val)) {
                    return NSAPI_ERROR_OK;
                }
            }
            break;
        case NSAPI_LINGER:
        case NSAPI_SNDBUF:
        case NSAPI_RCVBUF:
            break;
        case NSAPI_ADD_MEMBERSHIP:
            if (level == NSAPI_SOCKET && socket->proto == NSAPI_UDP) {
                char temp[64];
                nsapi_ip_mreq_t *mreq = (nsapi_ip_mreq_t *)optval;
                SocketAddress imr_interface(mreq->imr_interface);
                SocketAddress imr_multiaddr(mreq->imr_multiaddr);
                snprintf(temp, sizeof(temp) - 1, "\"%s\",\"%s\"", imr_interface.get_ip_address(), imr_multiaddr.get_ip_address());
                if (_esp->socket_setopt_s(socket->id, (LPSTR)"IP_ADD_MEMBERSHIP", (LPSTR)temp)) {
                    return NSAPI_ERROR_OK;
                }
            }
            break;
        case NSAPI_DROP_MEMBERSHIP:
            if (level == NSAPI_SOCKET && socket->proto == NSAPI_UDP) {
                char temp[64];
                nsapi_ip_mreq_t *mreq = (nsapi_ip_mreq_t *)optval;
                SocketAddress imr_interface(mreq->imr_interface);
                SocketAddress imr_multiaddr(mreq->imr_multiaddr);
                snprintf(temp, sizeof(temp) - 1, "\"%s\",\"%s\"", imr_interface.get_ip_address(), imr_multiaddr.get_ip_address());
                if (_esp->socket_setopt_s(socket->id, (LPSTR)"IP_DROP_MEMBERSHIP", (LPSTR)temp)) {
                    return NSAPI_ERROR_OK;
                }
            }
            break;
        case NSAPI_NODELAY:
            if (level == NSAPI_SOCKET && socket->proto == NSAPI_TCP) {
                long val = *(long *)optval;
                if (_esp->socket_setopt_i(socket->id, (LPSTR)"TCP_NODELAY", val)) {
                    return NSAPI_ERROR_OK;
                }
            }
            break;
        case NSAPI_SO_KEEPALIVE:
            if (level == NSAPI_SOCKET && socket->proto == NSAPI_TCP) {
                long val = *(long *)optval;
                if (_esp->socket_setopt_i(socket->id, (LPSTR)"SO_KEEPALIVE", val)) {
                    return NSAPI_ERROR_OK;
                }
            }
            break;
        case NSAPI_SO_RCVTIMEO:
            if (level == NSAPI_SOCKET && socket->proto == NSAPI_TCP) {
                long val = *(long *)optval;
                if (_esp->socket_setopt_i(socket->id, (LPSTR)"SO_RCVTIMEO", val)) {
                    return NSAPI_ERROR_OK;
                }
            }
            break;
        default:
            break;
    }

    return NSAPI_ERROR_UNSUPPORTED;
}

nsapi_error_t ESP32Stack::getsockopt(nsapi_socket_t handle, int level,
    int optname, void *optval, unsigned *optlen)
{
    struct esp32_socket *socket = (struct esp32_socket *)handle;

    if (!optval || !optlen) {
        return NSAPI_ERROR_PARAMETER;
    } else if (!socket) {
        return NSAPI_ERROR_NO_SOCKET;
    }

    switch (optname) {
        case NSAPI_KEEPALIVE:
            if (level == NSAPI_SOCKET && socket->proto == NSAPI_TCP) {
                if (*optlen > sizeof(int)) {
                    *optlen = sizeof(int);
                }
                memcpy(optval, &(socket->keepalive), *optlen);
                return NSAPI_ERROR_OK;
            }
        case NSAPI_REUSEADDR:
            if (level == NSAPI_SOCKET && socket->proto == NSAPI_TCP) {
                long val = *(long *)optval;
                if (_esp->socket_getopt_i(socket->id, (LPSTR)"SO_REUSEADDR", &val)) {
                    return NSAPI_ERROR_OK;
                }
            }
            break;
        case NSAPI_KEEPIDLE:
            if (level == NSAPI_SOCKET && socket->proto == NSAPI_TCP) {
                long val = *(long *)optval;
                if (_esp->socket_getopt_i(socket->id, (LPSTR)"TCP_KEEPIDLE", &val)) {
                    return NSAPI_ERROR_OK;
                }
            }
            break;
        case NSAPI_KEEPINTVL:
            if (level == NSAPI_SOCKET && socket->proto == NSAPI_TCP) {
                long val = *(long *)optval;
                if (_esp->socket_getopt_i(socket->id, (LPSTR)"TCP_KEEPINTVL", &val)) {
                    return NSAPI_ERROR_OK;
                }
            }
            break;
        case NSAPI_LINGER:
        case NSAPI_SNDBUF:
        case NSAPI_RCVBUF:
            break;
        case NSAPI_NODELAY:
            if (level == NSAPI_SOCKET && socket->proto == NSAPI_TCP) {
                long val = *(long *)optval;
                if (_esp->socket_getopt_i(socket->id, (LPSTR)"TCP_NODELAY", &val)) {
                    return NSAPI_ERROR_OK;
                }
            }
            break;
        case NSAPI_ERROR:
            if (level == NSAPI_SOCKET && socket->proto == NSAPI_TCP) {
                long val = *(long *)optval;
                if (_esp->socket_getopt_i(socket->id, (LPSTR)"SO_ERROR", &val)) {
                    return NSAPI_ERROR_OK;
                }
            }
            break;
        case NSAPI_SO_KEEPALIVE:
            if (level == NSAPI_SOCKET && socket->proto == NSAPI_TCP) {
                long val = *(long *)optval;
                if (_esp->socket_getopt_i(socket->id, (LPSTR)"SO_KEEPALIVE", &val)) {
                    return NSAPI_ERROR_OK;
                }
            }
            break;
        case NSAPI_SO_RCVTIMEO:
            if (level == NSAPI_SOCKET && socket->proto == NSAPI_TCP) {
                long val = *(long *)optval;
                if (_esp->socket_getopt_i(socket->id, (LPSTR)"SO_RCVTIMEO", &val)) {
                    return NSAPI_ERROR_OK;
                }
            }
            break;
        default:
            break;
    }

    return NSAPI_ERROR_UNSUPPORTED;
}

