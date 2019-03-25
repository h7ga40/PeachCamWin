/* Socket
 * Copyright (c) 2015 ARM Limited
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
#include "SocketAddress.h"
#include "NetworkInterface.h"
#include "NetworkStack.h"
#include <string.h>
#include <stdio.h>
//#include "frameworks/nanostack-libservice/mbed-client-libservice/ip6string.h"
void stoip6(const char *ip6addr, size_t len, void *dest);
uint_fast8_t ip6tos(const void *ip6addr, char *p);
uint8_t *bitcopy(uint8_t *__restrict dst, const uint8_t *__restrict src, uint_fast8_t bits);

static bool ipv4_is_valid(const char *addr)
{
    int i = 0;

    // Check each digit for [0-9.]
    for (; addr[i]; i++) {
        if (!(addr[i] >= '0' && addr[i] <= '9') && addr[i] != '.') {
            return false;
        }
    }

    // Ending with '.' garuntees host
    if (i > 0 && addr[i - 1] == '.') {
        return false;
    }

    return true;
}

static bool ipv6_is_valid(const char *addr)
{
    // Check each digit for [0-9a-fA-F:]
    // Must also have at least 2 colons
    int colons = 0;
    for (int i = 0; addr[i]; i++) {
        if (!(addr[i] >= '0' && addr[i] <= '9') &&
                !(addr[i] >= 'a' && addr[i] <= 'f') &&
                !(addr[i] >= 'A' && addr[i] <= 'F') &&
                addr[i] != ':') {
            return false;
        }
        if (addr[i] == ':') {
            colons++;
        }
    }

    return colons >= 2;
}

static void ipv4_from_address(uint8_t *bytes, const char *addr)
{
    int count = 0;
    int i = 0;

    for (; count < NSAPI_IPv4_BYTES; count++) {
        unsigned d;
        // Not using %hh, since it might be missing in newlib-based toolchains.
        // See also: https://git.io/vxiw5
        int scanned = sscanf(&addr[i], "%u", &d);
        if (scanned < 1) {
            return;
        }

        bytes[count] = static_cast<uint8_t>(d);

        for (; addr[i] != '.'; i++) {
            if (!addr[i]) {
                return;
            }
        }

        i++;
    }
}

static void ipv6_from_address(uint8_t *bytes, const char *addr)
{
    stoip6(addr, strlen(addr), bytes);
}

static void ipv4_to_address(char *addr, const uint8_t *bytes)
{
    sprintf(addr, "%d.%d.%d.%d", bytes[0], bytes[1], bytes[2], bytes[3]);
}

static void ipv6_to_address(char *addr, const uint8_t *bytes)
{
    ip6tos(bytes, addr);
}


SocketAddress::SocketAddress(nsapi_addr_t addr, uint16_t port)
{
    _ip_address = NULL;
    set_addr(addr);
    set_port(port);
}

SocketAddress::SocketAddress(const char *addr, uint16_t port)
{
    _ip_address = NULL;
    set_ip_address(addr);
    set_port(port);
}

SocketAddress::SocketAddress(const void *bytes, nsapi_version_t version, uint16_t port)
{
    _ip_address = NULL;
    set_ip_bytes(bytes, version);
    set_port(port);
}

SocketAddress::SocketAddress(const SocketAddress &addr)
{
    _ip_address = NULL;
    set_addr(addr.get_addr());
    set_port(addr.get_port());
}

bool SocketAddress::set_ip_address(const char *addr)
{
    delete[] _ip_address;
    _ip_address = NULL;

    if (addr && ipv4_is_valid(addr)) {
        _addr.version = NSAPI_IPv4;
        ipv4_from_address(_addr.bytes, addr);
        return true;
    } else if (addr && ipv6_is_valid(addr)) {
        _addr.version = NSAPI_IPv6;
        ipv6_from_address(_addr.bytes, addr);
        return true;
    } else {
        _addr = nsapi_addr_t();
        return false;
    }
}

void SocketAddress::set_ip_bytes(const void *bytes, nsapi_version_t version)
{
    nsapi_addr_t addr;

    addr = nsapi_addr_t();
    addr.version = version;
    if (version == NSAPI_IPv6) {
        memcpy(addr.bytes, bytes, NSAPI_IPv6_BYTES);
    } else if (version == NSAPI_IPv4) {
        memcpy(addr.bytes, bytes, NSAPI_IPv4_BYTES);
    }
    set_addr(addr);
}

void SocketAddress::set_addr(nsapi_addr_t addr)
{
    delete[] _ip_address;
    _ip_address = NULL;
    _addr = addr;
}

void SocketAddress::set_port(uint16_t port)
{
    _port = port;
}

const char *SocketAddress::get_ip_address() const
{
    if (_addr.version == NSAPI_UNSPEC) {
        return NULL;
    }

    if (!_ip_address) {
        _ip_address = new char[NSAPI_IP_SIZE];
        if (_addr.version == NSAPI_IPv4) {
            ipv4_to_address(_ip_address, _addr.bytes);
        } else if (_addr.version == NSAPI_IPv6) {
            ipv6_to_address(_ip_address, _addr.bytes);
        }
    }

    return _ip_address;
}

const void *SocketAddress::get_ip_bytes() const
{
    return _addr.bytes;
}

nsapi_version_t SocketAddress::get_ip_version() const
{
    return _addr.version;
}

nsapi_addr_t SocketAddress::get_addr() const
{
    return _addr;
}

uint16_t SocketAddress::get_port() const
{
    return _port;
}

SocketAddress::operator bool() const
{
    if (_addr.version == NSAPI_IPv4) {
        for (int i = 0; i < NSAPI_IPv4_BYTES; i++) {
            if (_addr.bytes[i]) {
                return true;
            }
        }

        return false;
    } else if (_addr.version == NSAPI_IPv6) {
        for (int i = 0; i < NSAPI_IPv6_BYTES; i++) {
            if (_addr.bytes[i]) {
                return true;
            }
        }

        return false;
    } else {
        return false;
    }
}

SocketAddress &SocketAddress::operator=(const SocketAddress &addr)
{
    delete[] _ip_address;
    _ip_address = NULL;
    set_addr(addr.get_addr());
    set_port(addr.get_port());
    return *this;
}

bool operator==(const SocketAddress &a, const SocketAddress &b)
{
    if (!a && !b) {
        return true;
    } else if (a._addr.version != b._addr.version) {
        return false;
    } else if (a._addr.version == NSAPI_IPv4) {
        return memcmp(a._addr.bytes, b._addr.bytes, NSAPI_IPv4_BYTES) == 0;
    } else if (a._addr.version == NSAPI_IPv6) {
        return memcmp(a._addr.bytes, b._addr.bytes, NSAPI_IPv6_BYTES) == 0;
    }

    MBED_UNREACHABLE;

    return false;
}

bool operator!=(const SocketAddress &a, const SocketAddress &b)
{
    return !(a == b);
}

void SocketAddress::_SocketAddress(NetworkStack *iface, const char *host, uint16_t port)
{
    _ip_address = NULL;

    // gethostbyname must check for literals, so can call it directly
    int err = iface->gethostbyname(host, this);
    _port = port;
    if (err) {
        _addr = nsapi_addr_t();
        _port = 0;
    }
}

SocketAddress::~SocketAddress()
{
    delete[] _ip_address;
}

static uint16_t hex(const char *p);
uint8_t *common_write_16_bit(uint16_t value, uint8_t ptr[2])
{
	*ptr++ = value >> 8;
	*ptr++ = value;
	return ptr;
}
/**
 * Convert numeric IPv6 address string to a binary.
 * IPv4 tunnelling addresses are not covered.
 * \param ip6addr IPv6 address in string format.
 * \param len Length of ipv6 string.
 * \param dest buffer for address. MUST be 16 bytes.
 */
void stoip6(const char *ip6addr, size_t len, void *dest)
{
	uint8_t *addr;
	const char *p, *q;
	int_fast8_t field_no, coloncolon = -1;

	addr = (uint8_t *)dest;

	if (len > 39) { // Too long, not possible. We do not support IPv4-mapped IPv6 addresses
		return;
	}

	// First go forward the string, until end, noting :: position if any
	for (field_no = 0, p = ip6addr; (len > (size_t)(p - ip6addr)) && *p && field_no < 8; p = q + 1) {
		q = p;
		// Seek for ':' or end
		while (*q && (*q != ':')) {
			q++;
		}
		//Convert and write this part, (high-endian AKA network byte order)
		addr = common_write_16_bit(hex(p), addr);
		field_no++;
		//Check if we reached "::"
		if ((len > (size_t)(q - ip6addr)) && *q && (q[0] == ':') && (q[1] == ':')) {
			coloncolon = field_no;
			q++;
		}
	}

	if (coloncolon != -1) {
		/* Insert zeros in the appropriate place */
		uint_fast8_t head_size = 2 * coloncolon;
		uint_fast8_t inserted_size = 2 * (8 - field_no);
		uint_fast8_t tail_size = 16 - head_size - inserted_size;
		addr = (uint8_t *)dest;
		memmove(addr + head_size + inserted_size, addr + head_size, tail_size);
		memset(addr + head_size, 0, inserted_size);
	}
	else if (field_no != 8) {
	 /* Should really report an error if we didn't get 8 fields */
		memset(addr, 0, 16 - field_no * 2);
	}
}

unsigned char  sipv6_prefixlength(const char *ip6addr)
{
	const char *ptr = strchr(ip6addr, '/');
	if (ptr) {
		return (unsigned char)strtoul(ptr + 1, 0, 10);
	}
	return 0;
}

static uint16_t hex(const char *p)
{
	uint16_t val = 0;

	for (;;) {
		char c = *p++;
		if ((c >= '0') && (c <= '9')) {
			val = (val << 4) | (c - '0');
		}
		else if ((c >= 'A') && (c <= 'F')) {
			val = (val << 4) | (10 + (c - 'A'));
		}
		else if ((c >= 'a') && (c <= 'f')) {
			val = (val << 4) | (10 + (c - 'a'));
		}
		else {
			break; // Non hex character
		}
	}
	return val;
}

/**
 * Print binary IPv6 address to a string.
 * String must contain enough room for full address, 40 bytes exact.
 * IPv4 tunneling addresses are not covered.
 * \param addr IPv6 address.
 * \p buffer to write string to.
 */
uint_fast8_t ip6tos(const void *ip6addr, char *p)
{
	char *p_orig = p;
	uint_fast8_t zero_start = 255, zero_len = 1;
	const uint8_t *addr = (const uint8_t *)ip6addr;
	uint_fast16_t part;

	/* Follow RFC 5952 - pre-scan for longest run of zeros */
	for (uint_fast8_t n = 0; n < 8; n++) {
		part = *addr++;
		part = (part << 8) | *addr++;
		if (part != 0) {
			continue;
		}

		/* We're at the start of a run of zeros - scan to non-zero (or end) */
		uint_fast8_t n0 = n;
		for (n = n0 + 1; n < 8; n++) {
			part = *addr++;
			part = (part << 8) | *addr++;
			if (part != 0) {
				break;
			}
		}

		/* Now n0->initial zero of run, n->after final zero in run. Is this the
		 * longest run yet? If equal, we stick with the previous one - RFC 5952
		 * S4.2.3. Note that zero_len being initialised to 1 stops us
		 * shortening a 1-part run (S4.2.2.)
		 */
		if (n - n0 > zero_len) {
			zero_start = n0;
			zero_len = n - n0;
		}

		/* Continue scan for initial zeros from part n+1 - we've already
		 * consumed part n, and know it's non-zero. */
	}

	/* Now go back and print, jumping over any zero run */
	addr = (const uint8_t *)ip6addr;
	for (uint_fast8_t n = 0; n < 8;) {
		if (n == zero_start) {
			if (n == 0) {
				*p++ = ':';
			}
			*p++ = ':';
			addr += 2 * zero_len;
			n += zero_len;
			continue;
		}

		part = *addr++;
		part = (part << 8) | *addr++;
		n++;

		p += sprintf(p, "%x", part);

		/* One iteration writes "part:" rather than ":part", and has the
		 * explicit check for n == 8 below, to allow easy extension for
		 * IPv4-in-IPv6-type addresses ("xxxx::xxxx:a.b.c.d"): we'd just
		 * run the same loop for 6 parts, and output would then finish with the
		 * required : or ::, ready for "a.b.c.d" to be tacked on.
		 */
		if (n != 8) {
			*p++ = ':';
		}
	}
	*p = '\0';

	// Return length of generated string, excluding the terminating null character
	return p - p_orig;
}

uint_fast8_t ip6_prefix_tos(const uint8_t *prefix, uint_fast8_t prefix_len, char *p)
{
	char *wptr = p;
	uint8_t addr[16] = { 0 };

	if (prefix_len > 128) {
		return 0;
	}

	// Generate prefix part of the string
	bitcopy(addr, prefix, prefix_len);
	wptr += ip6tos(addr, wptr);
	// Add the prefix length part of the string
	wptr += sprintf(wptr, "/%u", prefix_len);

	// Return total length of generated string
	return wptr - p;
}

/* Returns mask for <split_value> (0-8) most-significant bits of a byte */
static inline uint8_t context_split_mask(uint_fast8_t split_value)
{
	return (uint8_t)-(0x100u >> split_value);
}

uint8_t *bitcopy(uint8_t *__restrict dst, const uint8_t *__restrict src, uint_fast8_t bits)
{
	uint_fast8_t bytes = bits / 8;
	bits %= 8;

	if (bytes) {
		dst = (uint8_t *)memcpy(dst, src, bytes) + bytes;
		src += bytes;
	}

	if (bits) {
		uint_fast8_t split_bit = context_split_mask(bits);
		*dst = (*src & split_bit) | (*dst & ~split_bit);
	}

	return dst;
}
