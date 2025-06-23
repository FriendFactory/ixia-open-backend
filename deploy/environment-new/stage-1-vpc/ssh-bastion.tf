data "aws_vpc" "vpc" {
  filter {
    name   = "tag:Name"
    values = [var.env]
  }
}

data "aws_subnet_ids" "public" {
  vpc_id = data.aws_vpc.vpc.id
  tags = {
    Tier = "Public"
  }
}

resource "aws_instance" "ssh_bastion_host" {
  ami           = "ami-07df274a488ca9195"
  instance_type = "t2.nano"
  key_name      = var.ssh_key_pair_name

  subnet_id                   = sort(data.aws_subnet_ids.public.ids)[0]
  associate_public_ip_address = true

  tags = {
    Name = "${var.env}-ssh-bastion"
  }

}

data "aws_route53_zone" "frever_api" {
  name = "frever-api.com"
}

resource "aws_route53_record" "ssh_bastion_host_dns_record" {
  zone_id = data.aws_route53_zone.frever_api.zone_id
  name    = "ssh-${var.env}.frever-api.com"
  type    = "A"
  ttl     = "300"
  records = [aws_instance.ssh_bastion_host.public_ip]
}
